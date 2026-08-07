import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  OnInit,
  output,
  signal,
  ViewChild,
  ElementRef,
  afterNextRender,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { marked } from 'marked';
import { ChatService, ConversationSummaryDto, ChatMessageResponseDto } from './chat.service';

interface VisibleMessage {
  id: number;
  role: 'user' | 'assistant';
  content: string;
  /** HTML parseado de markdown (solo mensajes assistant). */
  contentHtml: string;
  createdAt: string;
  toolAction?: boolean; // indica que justo antes hubo mensajes tool
}

@Component({
  selector: 'app-chat-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    NzDrawerModule,
    NzButtonModule,
    NzIconModule,
    NzInputModule,
    NzSpinModule,
    NzEmptyModule,
    NzPopconfirmModule,
    NzDividerModule,
    NzTooltipModule,
  ],
  styles: [`
    .chat-layout {
      display: flex;
      height: calc(100vh - 55px);
      gap: 0;
    }
    .conv-list {
      width: 220px;
      flex: 0 0 220px;
      border-right: 1px solid #f0f0f0;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }
    .conv-list-header {
      padding: 10px 12px 8px;
      flex: 0 0 auto;
      border-bottom: 1px solid #f0f0f0;
    }
    .conv-list-items {
      flex: 1 1 auto;
      overflow-y: auto;
    }
    .conv-item {
      padding: 9px 12px;
      cursor: pointer;
      border-left: 3px solid transparent;
      transition: background 150ms;
      border-bottom: 1px solid #fafafa;
    }
    .conv-item:hover {
      background: #f5f5f5;
    }
    .conv-item.active {
      background: #e6f7ff;
      border-left-color: #1890ff;
    }
    .conv-title {
      font-size: 13px;
      font-weight: 600;
      color: #262626;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .conv-meta {
      font-size: 11px;
      color: #8c8c8c;
      margin-top: 2px;
    }
    .chat-area {
      flex: 1 1 auto;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }
    .messages-container {
      flex: 1 1 auto;
      overflow-y: auto;
      padding: 16px 20px;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .msg-row {
      display: flex;
      flex-direction: column;
    }
    .msg-row.user {
      align-items: flex-end;
    }
    .msg-row.assistant {
      align-items: flex-start;
    }
    .msg-bubble {
      max-width: 80%;
      padding: 9px 13px;
      border-radius: 12px;
      font-size: 13.5px;
      line-height: 1.55;
      word-break: break-word;
    }
    .msg-row.user .msg-bubble {
      background: #1890ff;
      color: #fff;
      border-bottom-right-radius: 3px;
      white-space: pre-wrap;
    }
    .msg-row.assistant .msg-bubble {
      background: #f0f0f0;
      color: #262626;
      border-bottom-left-radius: 3px;
    }
    /* Estilos básicos para el HTML parseado del asistente */
    .msg-row.assistant .msg-bubble p { margin: 0 0 8px; }
    .msg-row.assistant .msg-bubble p:last-child { margin-bottom: 0; }
    .msg-row.assistant .msg-bubble ul,
    .msg-row.assistant .msg-bubble ol { margin: 4px 0 8px 16px; padding: 0; }
    .msg-row.assistant .msg-bubble li { margin-bottom: 2px; }
    .msg-row.assistant .msg-bubble strong { font-weight: 700; }
    .msg-row.assistant .msg-bubble em { font-style: italic; }
    .msg-row.assistant .msg-bubble code {
      background: #e0e0e0;
      border-radius: 3px;
      padding: 1px 4px;
      font-size: 12.5px;
      font-family: monospace;
    }
    .msg-row.assistant .msg-bubble pre {
      background: #e0e0e0;
      border-radius: 6px;
      padding: 8px 10px;
      overflow-x: auto;
      margin: 6px 0;
    }
    .msg-row.assistant .msg-bubble pre code {
      background: none;
      padding: 0;
    }
    .msg-time {
      font-size: 10px;
      color: #bfbfbf;
      margin-top: 3px;
    }
    .tool-indicator {
      font-size: 11px;
      color: #8c8c8c;
      padding: 2px 0;
      align-self: flex-start;
    }
    .input-bar {
      flex: 0 0 auto;
      padding: 10px 16px;
      border-top: 1px solid #f0f0f0;
      display: flex;
      gap: 8px;
      align-items: flex-end;
    }
    .empty-chat {
      flex: 1 1 auto;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  `],
  template: `
    <nz-drawer
      [nzVisible]="open()"
      [nzWidth]="700"
      nzTitle="Chat IA"
      [nzClosable]="true"
      nzPlacement="right"
      (nzOnClose)="closed.emit()"
    >
      <ng-container *nzDrawerContent>
        <nz-spin [nzSpinning]="loadingConvs()">
          <div class="chat-layout">

            <!-- Panel izquierdo: lista de conversaciones -->
            <div class="conv-list">
              <div class="conv-list-header">
                <button nz-button nzType="primary" nzSize="small" style="width:100%" (click)="startNewConversation()">
                  <span nz-icon nzType="plus"></span> Nueva conversación
                </button>
              </div>

              <div class="conv-list-items">
                @if (conversations().length === 0 && !loadingConvs()) {
                  <div style="padding:20px 12px;text-align:center">
                    <nz-empty nzNotFoundContent="Sin conversaciones" [nzNotFoundImage]="'simple'" />
                  </div>
                }
                @for (conv of conversations(); track conv.id) {
                  <div
                    class="conv-item"
                    [class.active]="activeConvId() === conv.id"
                    (click)="selectConversation(conv.id)"
                  >
                    <div style="display:flex;align-items:flex-start;justify-content:space-between;gap:4px">
                      <div class="conv-title" [title]="conv.title">{{ conv.title }}</div>
                      <button
                        nz-button nzType="text" nzSize="small" nzDanger
                        nz-popconfirm nzPopconfirmTitle="¿Eliminar esta conversación?"
                        (nzOnConfirm)="deleteConversation(conv.id)"
                        (click)="$event.stopPropagation()"
                        style="flex:0 0 auto;height:20px;width:20px;padding:0;min-width:0;line-height:20px"
                        nz-tooltip nzTooltipTitle="Eliminar"
                      >
                        <span nz-icon nzType="delete" style="font-size:11px"></span>
                      </button>
                    </div>
                    <div class="conv-meta">{{ formatDate(conv.updatedAt) }} · {{ conv.messageCount }} msg</div>
                  </div>
                }
              </div>
            </div>

            <!-- Panel derecho: mensajes -->
            <div class="chat-area">
              @if (activeConvId() === null) {
                <div class="empty-chat">
                  <nz-empty nzNotFoundContent="Selecciona o crea una conversación" [nzNotFoundImage]="'simple'" />
                </div>
              } @else {
                <div class="messages-container" #messagesContainer>
                  @if (loadingMsgs()) {
                    <div style="text-align:center;padding:40px"><nz-spin /></div>
                  } @else {
                    @if (visibleMessages().length === 0) {
                      <div style="text-align:center;padding:20px">
                        <nz-empty nzNotFoundContent="Sin mensajes todavía" [nzNotFoundImage]="'simple'" />
                      </div>
                    }
                    @for (msg of visibleMessages(); track msg.id) {
                      @if (msg.toolAction) {
                        <div class="tool-indicator">🔧 acción ejecutada</div>
                      }
                      <div class="msg-row" [class]="msg.role">
                        @if (msg.role === 'assistant') {
                          <div class="msg-bubble" [innerHTML]="msg.contentHtml"></div>
                        } @else {
                          <div class="msg-bubble">{{ msg.content }}</div>
                        }
                        <div class="msg-time">{{ formatTime(msg.createdAt) }}</div>
                      </div>
                    }
                    @if (sending()) {
                      <div class="msg-row assistant">
                        <div class="msg-bubble" style="min-width:60px">
                          <nz-spin nzSimple [nzSize]="'small'"></nz-spin>
                        </div>
                      </div>
                    }
                  }
                </div>

                <div class="input-bar">
                  <textarea
                    nz-input
                    [nzAutosize]="{ minRows: 1, maxRows: 4 }"
                    placeholder="Escribe un mensaje…"
                    [(ngModel)]="inputText"
                    [disabled]="sending()"
                    (keydown.enter)="onEnterKey($event)"
                    style="resize:none;flex:1 1 auto"
                  ></textarea>
                  <button
                    nz-button nzType="primary"
                    [disabled]="sending() || !inputText.trim()"
                    (click)="sendMessage()"
                  >
                    <span nz-icon nzType="send"></span>
                  </button>
                </div>
              }
            </div>

          </div>
        </nz-spin>
      </ng-container>
    </nz-drawer>
  `,
})
export class ChatPanelComponent {
  readonly open = input.required<boolean>();
  readonly closed = output<void>();

  @ViewChild('messagesContainer') private messagesContainerRef?: ElementRef<HTMLElement>;

  private readonly chatService = inject(ChatService);
  private readonly message = inject(NzMessageService);

  // Estado
  readonly conversations = signal<ConversationSummaryDto[]>([]);
  readonly activeConvId = signal<number | null>(null);
  readonly rawMessages = signal<ChatMessageResponseDto[]>([]);
  readonly loadingConvs = signal(false);
  readonly loadingMsgs = signal(false);
  readonly sending = signal(false);

  inputText = '';

  // Solo mensajes user/assistant visibles; marca toolAction en el primero
  // que viene después de una racha de mensajes tool.
  readonly visibleMessages = computed<VisibleMessage[]>(() => {
    const raw = this.rawMessages();
    const result: VisibleMessage[] = [];
    let pendingToolAction = false;

    for (const msg of raw) {
      if (msg.role === 'tool') {
        pendingToolAction = true;
        continue;
      }
      if (msg.role === 'user' || msg.role === 'assistant') {
        const content = msg.content ?? '';
        result.push({
          id: msg.id,
          role: msg.role,
          content,
          contentHtml: msg.role === 'assistant'
            ? (marked.parse(content, { async: false }) as string)
            : '',
          createdAt: msg.createdAt,
          toolAction: pendingToolAction,
        });
        pendingToolAction = false;
      }
    }
    return result;
  });

  constructor() {
    // Carga las conversaciones cuando el panel se abre
    effect(() => {
      if (this.open()) {
        this.loadConversations();
      }
    });

    // Scroll al fondo cuando llegan mensajes nuevos o mientras se envía
    effect(() => {
      // Accedemos a las señales reactivas para que el effect se re-ejecute
      this.visibleMessages();
      this.sending();
      this.scrollToBottom();
    });
  }

  private loadConversations(): void {
    this.loadingConvs.set(true);
    this.chatService.listConversations().subscribe({
      next: result => {
        this.conversations.set(result.items);
        this.loadingConvs.set(false);
      },
      error: () => {
        this.loadingConvs.set(false);
        // No mostramos error toast para listado fallido, simplemente vacío
      },
    });
  }

  selectConversation(id: number): void {
    if (this.activeConvId() === id) return;
    this.activeConvId.set(id);
    this.rawMessages.set([]);
    this.loadingMsgs.set(true);
    this.chatService.getMessages(id).subscribe({
      next: msgs => {
        this.rawMessages.set(msgs);
        this.loadingMsgs.set(false);
      },
      error: () => {
        this.loadingMsgs.set(false);
        this.activeConvId.set(null);
      },
    });
  }

  startNewConversation(): void {
    const now = new Date();
    const title = `Chat ${now.toLocaleDateString('es-ES', { day: '2-digit', month: '2-digit', year: 'numeric' })} ${now.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })}`;
    this.chatService.createConversation(title).subscribe({
      next: ({ id }) => {
        // Recarga lista y abre la nueva conversación
        this.chatService.listConversations().subscribe({
          next: result => {
            this.conversations.set(result.items);
            this.selectConversation(id);
          },
        });
      },
      error: () => {
        this.message.error('No se pudo crear la conversación');
      },
    });
  }

  deleteConversation(id: number): void {
    this.chatService.deleteConversation(id).subscribe({
      next: () => {
        this.conversations.update(list => list.filter(c => c.id !== id));
        if (this.activeConvId() === id) {
          this.activeConvId.set(null);
          this.rawMessages.set([]);
        }
      },
      error: () => {
        this.message.error('No se pudo eliminar la conversación');
      },
    });
  }

  sendMessage(): void {
    const text = this.inputText.trim();
    if (!text || this.sending()) return;
    const convId = this.activeConvId();
    if (convId === null) return;

    // Añade el mensaje del usuario de forma optimista
    const tempUserMsg: ChatMessageResponseDto = {
      id: -Date.now(),
      role: 'user',
      content: text,
      toolCallsJson: null,
      toolName: null,
      toolCallId: null,
      createdAt: new Date().toISOString(),
    };
    this.rawMessages.update(msgs => [...msgs, tempUserMsg]);
    this.inputText = '';
    this.sending.set(true);

    this.chatService.sendMessage(convId, text).subscribe({
      next: result => {
        // Recarga mensajes reales desde el servidor (incluye tool messages del backend)
        this.chatService.getMessages(convId).subscribe({
          next: msgs => {
            this.rawMessages.set(msgs);
            // Actualiza el contador en la lista de conversaciones
            this.conversations.update(list =>
              list.map(c =>
                c.id === convId
                  ? { ...c, updatedAt: new Date().toISOString(), messageCount: msgs.length }
                  : c,
              ),
            );
          },
        });
        if (result.hitIterationLimit) {
          this.message.warning('El asistente alcanzó el límite de iteraciones. La respuesta puede estar incompleta.');
        }
        this.sending.set(false);
      },
      error: () => {
        // Restaura el texto para que el usuario pueda reintentar
        this.inputText = text;
        // Elimina el mensaje optimista temporal
        this.rawMessages.update(msgs => msgs.filter(m => m.id !== tempUserMsg.id));
        this.message.error('Error al enviar el mensaje. Puedes intentarlo de nuevo.');
        this.sending.set(false);
      },
    });
  }

  onEnterKey(event: Event): void {
    const ke = event as KeyboardEvent;
    // Enter sin Shift envía; Shift+Enter es salto de línea
    if (!ke.shiftKey) {
      ke.preventDefault();
      this.sendMessage();
    }
  }

  formatDate(dateStr: string): string {
    const d = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    if (diffDays === 0) return 'Hoy';
    if (diffDays === 1) return 'Ayer';
    return d.toLocaleDateString('es-ES', { day: '2-digit', month: '2-digit' });
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' });
  }

  private scrollToBottom(): void {
    // Usamos setTimeout para dejar que el DOM se actualice antes de hacer scroll
    setTimeout(() => {
      const el = this.messagesContainerRef?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    }, 0);
  }
}
