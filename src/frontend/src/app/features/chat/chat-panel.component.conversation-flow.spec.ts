/**
 * Tests del flujo de conversación instantánea de ChatPanelComponent:
 *  - deriveTitle (título derivado del primer mensaje)
 *  - envío sin conversación activa (creación perezosa)
 *  - error al crear la conversación (no se pierde el texto)
 *  - guarda de condición de carrera (respuesta tardía no pisa la vista actual)
 *  - startNewConversation (reset local, sin llamada al backend)
 *
 * No hay infraestructura de TestBed en este proyecto (vitest + happy-dom
 * zoneless, sin zone.js), así que instanciamos el componente dentro de un
 * EnvironmentInjector real —construido con `createApplication` para disponer
 * del scheduler de detección de cambios zoneless que requiere `effect()`—
 * inyectando mocks de ChatService y NzMessageService. Se importa
 * `@angular/compiler` para habilitar la compilación JIT de las dependencias
 * de Angular en el entorno de test.
 */
import '@angular/compiler';
import { describe, it, expect, beforeAll, afterAll, vi } from 'vitest';
import {
  ApplicationRef,
  createEnvironmentInjector,
  EnvironmentInjector,
  provideZonelessChangeDetection,
  runInInjectionContext,
} from '@angular/core';
import { createApplication } from '@angular/platform-browser';
import { of, Subject, throwError } from 'rxjs';
import { ChatPanelComponent } from './chat-panel.component';
import {
  ChatService,
  ChatMessageResponseDto,
  SendChatMessageResult,
} from './chat.service';
import { NzMessageService } from 'ng-zorro-antd/message';

interface ChatServiceMock {
  listConversations: ReturnType<typeof vi.fn>;
  createConversation: ReturnType<typeof vi.fn>;
  getMessages: ReturnType<typeof vi.fn>;
  sendMessage: ReturnType<typeof vi.fn>;
  deleteConversation: ReturnType<typeof vi.fn>;
}

interface MessageMock {
  error: ReturnType<typeof vi.fn>;
  warning: ReturnType<typeof vi.fn>;
  success: ReturnType<typeof vi.fn>;
}

let appRef: ApplicationRef;
let rootInjector: EnvironmentInjector;

beforeAll(async () => {
  appRef = await createApplication({ providers: [provideZonelessChangeDetection()] });
  rootInjector = appRef.injector.get(EnvironmentInjector);
});

afterAll(() => {
  appRef.destroy();
});

function makeChatMock(overrides: Partial<ChatServiceMock> = {}): ChatServiceMock {
  return {
    listConversations: vi.fn(() => of({ items: [], total: 0, page: 1, pageSize: 20 })),
    createConversation: vi.fn(() => of({ id: 1 })),
    getMessages: vi.fn(() => of([] as ChatMessageResponseDto[])),
    sendMessage: vi.fn(() =>
      of({ messageId: 1, assistantReply: 'ok', hitIterationLimit: false } as SendChatMessageResult),
    ),
    deleteConversation: vi.fn(() => of(void 0)),
    ...overrides,
  };
}

function makeMessageMock(): MessageMock {
  return { error: vi.fn(), warning: vi.fn(), success: vi.fn() };
}

function createComponent(chatMock: ChatServiceMock, msgMock: MessageMock): ChatPanelComponent {
  const inj = createEnvironmentInjector(
    [
      { provide: ChatService, useValue: chatMock },
      { provide: NzMessageService, useValue: msgMock },
    ],
    rootInjector,
  );
  return runInInjectionContext(inj, () => new ChatPanelComponent());
}

function msg(id: number, role: 'user' | 'assistant' | 'tool', content: string): ChatMessageResponseDto {
  return {
    id,
    role,
    content,
    toolCallsJson: null,
    toolName: null,
    toolCallId: null,
    createdAt: new Date().toISOString(),
  };
}

describe('ChatPanelComponent – deriveTitle', () => {
  let comp: ChatPanelComponent;
  // deriveTitle es privado; se accede por índice para no exponer API pública.
  const deriveTitle = (t: string): string =>
    (comp as unknown as { deriveTitle(text: string): string }).deriveTitle(t);

  beforeAll(() => {
    comp = createComponent(makeChatMock(), makeMessageMock());
  });

  it('toma la primera línea de un texto multilínea', () => {
    expect(deriveTitle('Primera línea\nsegunda línea\ntercera')).toBe('Primera línea');
  });

  it('devuelve un texto corto tal cual', () => {
    expect(deriveTitle('Hola mundo')).toBe('Hola mundo');
  });

  it('recorta a 60 caracteres y añade «…» en textos largos', () => {
    const long = 'a'.repeat(80);
    const title = deriveTitle(long);
    expect(title).toBe('a'.repeat(60) + '…');
    expect(title.length).toBe(61); // 60 chars + el carácter «…»
  });

  it('recorta espacios de la primera línea', () => {
    expect(deriveTitle('   hola   \nmás')).toBe('hola');
  });
});

describe('ChatPanelComponent – envío sin conversación activa (creación perezosa)', () => {
  it('crea la conversación con el título derivado y luego envía el mensaje con ese id', () => {
    const chat = makeChatMock({ createConversation: vi.fn(() => of({ id: 42 })) });
    const comp = createComponent(chat, makeMessageMock());

    comp.activeConvId.set(null);
    comp.inputText = 'Necesito el informe\nde cartera';
    comp.sendMessage();

    // 1º crea la conversación con el título derivado (primera línea)
    expect(chat.createConversation).toHaveBeenCalledTimes(1);
    expect(chat.createConversation).toHaveBeenCalledWith('Necesito el informe');

    // activeConvId queda al id devuelto
    expect(comp.activeConvId()).toBe(42);

    // 2º envía el mensaje original con ese id
    expect(chat.sendMessage).toHaveBeenCalledTimes(1);
    expect(chat.sendMessage).toHaveBeenCalledWith(42, 'Necesito el informe\nde cartera');
  });

  it('no crea conversación si el texto está vacío', () => {
    const chat = makeChatMock();
    const comp = createComponent(chat, makeMessageMock());
    comp.activeConvId.set(null);
    comp.inputText = '   ';
    comp.sendMessage();
    expect(chat.createConversation).not.toHaveBeenCalled();
    expect(chat.sendMessage).not.toHaveBeenCalled();
  });
});

describe('ChatPanelComponent – error al crear la conversación', () => {
  it('deja sending en false, muestra el error y conserva el texto escrito', () => {
    const chat = makeChatMock({
      createConversation: vi.fn(() => throwError(() => new Error('boom'))),
    });
    const msgMock = makeMessageMock();
    const comp = createComponent(chat, msgMock);

    comp.activeConvId.set(null);
    comp.inputText = 'Texto que no se debe perder';
    comp.sendMessage();

    expect(comp.sending()).toBe(false);
    expect(msgMock.error).toHaveBeenCalledWith('No se pudo crear la conversación');
    // El texto se conserva para reintentar
    expect(comp.inputText).toBe('Texto que no se debe perder');
    // No se llegó a enviar el mensaje
    expect(chat.sendMessage).not.toHaveBeenCalled();
  });
});

describe('ChatPanelComponent – guarda de condición de carrera', () => {
  it('una respuesta tardía no sobrescribe rawMessages si el usuario cambió de conversación', () => {
    const sendSubject = new Subject<SendChatMessageResult>();
    const chat = makeChatMock({
      sendMessage: vi.fn(() => sendSubject.asObservable()),
      // getMessages de la conversación 1 (respuesta tardía) devolvería estos mensajes
      getMessages: vi.fn(() => of([msg(1, 'user', 'hola'), msg(2, 'assistant', 'respuesta 1')])),
    });
    const comp = createComponent(chat, makeMessageMock());

    // Estamos en la conversación 1 y enviamos
    comp.activeConvId.set(1);
    comp.rawMessages.set([msg(1, 'user', 'hola')]);
    comp.inputText = 'hola';
    comp.sendMessage();
    expect(chat.sendMessage).toHaveBeenCalledWith(1, 'hola');

    // Mientras la petición está en curso, el usuario cambia a un borrador nuevo
    comp.startNewConversation();
    expect(comp.activeConvId()).toBeNull();
    const viewBeforeLateReply = comp.rawMessages();
    expect(viewBeforeLateReply).toEqual([]);

    // Llega la respuesta tardía de la conversación 1
    sendSubject.next({ messageId: 5, assistantReply: 'respuesta 1', hitIterationLimit: false });
    sendSubject.complete();

    // La vista actual (borrador) NO se ha sobrescrito con los mensajes de la conv 1
    expect(comp.rawMessages()).toEqual([]);
    // sending del borrador tampoco se reactiva por la respuesta ajena
    expect(comp.sending()).toBe(false);
  });

  it('la respuesta actualiza el contador de la conversación en la lista aunque no sea la activa', () => {
    const sendSubject = new Subject<SendChatMessageResult>();
    const serverMsgs = [msg(1, 'user', 'hola'), msg(2, 'assistant', 'r')];
    const chat = makeChatMock({
      sendMessage: vi.fn(() => sendSubject.asObservable()),
      getMessages: vi.fn(() => of(serverMsgs)),
    });
    const comp = createComponent(chat, makeMessageMock());

    comp.conversations.set([
      { id: 1, title: 'Conv 1', createdAt: '', updatedAt: '', messageCount: 1 },
    ]);
    comp.activeConvId.set(1);
    comp.inputText = 'hola';
    comp.sendMessage();

    // El usuario cambia de vista
    comp.startNewConversation();

    sendSubject.next({ messageId: 5, assistantReply: 'r', hitIterationLimit: false });
    sendSubject.complete();

    const conv = comp.conversations().find(c => c.id === 1)!;
    expect(conv.messageCount).toBe(serverMsgs.length);
  });

  it('la creación perezosa no secuestra la vista si el usuario navega a otra conversación mientras se crea', () => {
    const createSubject = new Subject<{ id: number }>();
    const chat = makeChatMock({
      createConversation: vi.fn(() => createSubject.asObservable()),
    });
    const comp = createComponent(chat, makeMessageMock());

    // Sin conversación activa: el usuario escribe y envía → se crea perezosamente.
    comp.activeConvId.set(null);
    comp.inputText = 'primer mensaje';
    comp.sendMessage();
    expect(chat.createConversation).toHaveBeenCalledWith('primer mensaje');

    // Antes de que resuelva createConversation, el usuario selecciona otra conversación.
    comp.selectConversation(99);
    expect(comp.activeConvId()).toBe(99);

    // Llega tarde la respuesta de createConversation (id 7).
    createSubject.next({ id: 7 });
    createSubject.complete();

    // No se secuestra la vista: seguimos en la conversación elegida por el usuario…
    expect(comp.activeConvId()).toBe(99);
    // …y NO se ha encadenado el envío (continueSend) del borrador abandonado.
    expect(chat.sendMessage).not.toHaveBeenCalled();
  });

  it('la creación perezosa tardía adopta el id creado si el usuario sigue en el mismo borrador', () => {
    // Con el mecanismo de token, si el usuario no abandona el borrador (no cambia
    // de conversación ni pulsa "Nueva conversación") el token no cambia y la
    // conversación recién creada se adopta y se encadena el envío correctamente.
    const createSubject = new Subject<{ id: number }>();
    const chat = makeChatMock({
      createConversation: vi.fn(() => createSubject.asObservable()),
    });
    const comp = createComponent(chat, makeMessageMock());

    comp.activeConvId.set(null);
    comp.inputText = 'hola';
    comp.sendMessage();

    createSubject.next({ id: 7 });
    createSubject.complete();

    expect(comp.activeConvId()).toBe(7);
    expect(chat.sendMessage).toHaveBeenCalledWith(7, 'hola');
  });

  it('pulsar "Nueva conversación" mientras se crea el borrador descarta el mensaje: no se envía', () => {
    // Escenario del hallazgo BLOQUEANTE de la ronda 2: el usuario escribe, envía,
    // se arrepiente y pulsa "Nueva conversación" antes de que resuelva la
    // creación. startNewConversation deja activeConvId en null (igual que antes),
    // así que solo el token distingue "borrador abandonado" de "mismo borrador".
    const createSubject = new Subject<{ id: number }>();
    const chat = makeChatMock({
      createConversation: vi.fn(() => createSubject.asObservable()),
    });
    const comp = createComponent(chat, makeMessageMock());

    comp.activeConvId.set(null);
    comp.inputText = 'hola';
    comp.sendMessage();
    expect(chat.createConversation).toHaveBeenCalledWith('hola');

    // El usuario descarta el borrador con "Nueva conversación" (activeConvId sigue null).
    comp.startNewConversation();
    expect(comp.activeConvId()).toBeNull();

    // Llega tarde la respuesta de createConversation (id 7).
    createSubject.next({ id: 7 });
    createSubject.complete();

    // El mensaje descartado NO se envía al backend…
    expect(chat.sendMessage).not.toHaveBeenCalled();
    // …ni se adopta la conversación creada, ni reaparece el mensaje en la vista.
    expect(comp.activeConvId()).toBeNull();
    expect(comp.rawMessages()).toEqual([]);
  });
});

describe('ChatPanelComponent – selectConversation y flag sending', () => {
  it('reseleccionar la conversación ya activa NO resetea sending (evita double-submit)', () => {
    const chat = makeChatMock();
    const comp = createComponent(chat, makeMessageMock());

    comp.activeConvId.set(5);
    comp.sending.set(true); // envío en curso en la conversación 5

    comp.selectConversation(5); // clic sobre la misma conversación activa

    // sending NO se resetea: la petición sigue en vuelo, no debe reabrirse el composer
    expect(comp.sending()).toBe(true);
    // no se recargan mensajes (guard de misma conversación)
    expect(chat.getMessages).not.toHaveBeenCalled();
  });

  it('seleccionar una conversación distinta SÍ resetea sending', () => {
    const chat = makeChatMock();
    const comp = createComponent(chat, makeMessageMock());

    comp.activeConvId.set(5);
    comp.sending.set(true);

    comp.selectConversation(6); // conversación distinta

    expect(comp.sending()).toBe(false);
    expect(comp.activeConvId()).toBe(6);
    expect(chat.getMessages).toHaveBeenCalledWith(6);
  });
});

describe('ChatPanelComponent – startNewConversation', () => {
  it('NO llama al backend y resetea el estado local', () => {
    const chat = makeChatMock();
    const comp = createComponent(chat, makeMessageMock());

    comp.activeConvId.set(7);
    comp.rawMessages.set([msg(1, 'user', 'algo')]);
    comp.inputText = 'texto a medias';
    comp.sending.set(true);

    comp.startNewConversation();

    expect(chat.createConversation).not.toHaveBeenCalled();
    expect(chat.listConversations).not.toHaveBeenCalled();
    expect(comp.activeConvId()).toBeNull();
    expect(comp.rawMessages()).toEqual([]);
    expect(comp.inputText).toBe('');
    expect(comp.sending()).toBe(false);
  });
});
