/**
 * Test de regresión: verifica que el pipeline markdown→HTML que usa
 * ChatPanelComponent en producción renderiza correctamente imágenes markdown
 * generadas por las tools del backend (p. ej. gráficos de charts).
 *
 * Pipeline real del componente:
 *   msg.content  →  marked.parse(content, { async: false })  →  contentHtml
 *   <div [innerHTML]="msg.contentHtml">  →  Angular sanitiza con ɵ_sanitizeHtml
 *
 * Se usa ɵ_sanitizeHtml (función interna de @angular/core) porque es
 * exactamente la función que Angular invoca al procesar [innerHTML].
 */
import { describe, it, expect } from 'vitest';
import { marked } from 'marked';
// ɵ_sanitizeHtml es la función interna que Angular usa para [innerHTML].
// Está expuesta (con prefijo ɵ) para facilitar precisamente este tipo de test.
import { ɵ_sanitizeHtml as sanitizeHtml } from '@angular/core';

const CHART_URL = 'https://ejemplo.test/api/chat/charts/abc123';
const MARKDOWN = `![gráfico](${CHART_URL})`;

describe('ChatPanelComponent – renderizado de imágenes markdown del backend', () => {
  it('marked.parse convierte la imagen markdown a etiqueta <img>', () => {
    const html = marked.parse(MARKDOWN, { async: false }) as string;
    expect(html).toContain('<img');
    expect(html).toContain(CHART_URL);
  });

  it('el sanitizador de Angular ([innerHTML]) no elimina el <img> con src https', () => {
    const rawHtml = marked.parse(MARKDOWN, { async: false }) as string;

    // ɵ_sanitizeHtml necesita un documento DOM (happy-dom lo provee a través de
    // la variable global `document` inyectada por el entorno de Vitest).
    const sanitized = sanitizeHtml(document, rawHtml);

    expect(sanitized).toContain('<img');
    expect(sanitized).toContain(CHART_URL);
  });

  it('el <img> aparece en el DOM tras asignar el HTML sanitizado a innerHTML', () => {
    const rawHtml = marked.parse(MARKDOWN, { async: false }) as string;
    const sanitized = sanitizeHtml(document, rawHtml);

    // Simula exactamente lo que hace Angular con [innerHTML]="msg.contentHtml"
    const container = document.createElement('div');
    container.innerHTML = sanitized;

    const img = container.querySelector('img');
    expect(img).not.toBeNull();
    expect(img!.getAttribute('src')).toBe(CHART_URL);
  });
});
