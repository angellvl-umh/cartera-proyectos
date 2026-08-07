import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ConversationSummaryDto {
  id: number;
  title: string;
  createdAt: string;
  updatedAt: string;
  messageCount: number;
}

export interface ChatMessageResponseDto {
  id: number;
  role: 'user' | 'assistant' | 'tool';
  content: string | null;
  toolCallsJson: string | null;
  toolName: string | null;
  toolCallId: string | null;
  createdAt: string;
}

export interface SendChatMessageResult {
  messageId: number;
  assistantReply: string;
  hitIterationLimit: boolean;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);

  listConversations(page = 1, pageSize = 20): Observable<PagedResult<ConversationSummaryDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<ConversationSummaryDto>>('/api/chat/conversations', { params });
  }

  createConversation(title: string): Observable<{ id: number }> {
    return this.http.post<{ id: number }>('/api/chat/conversations', { title });
  }

  getMessages(conversationId: number): Observable<ChatMessageResponseDto[]> {
    return this.http.get<ChatMessageResponseDto[]>(`/api/chat/conversations/${conversationId}/messages`);
  }

  sendMessage(conversationId: number, text: string): Observable<SendChatMessageResult> {
    return this.http.post<SendChatMessageResult>(`/api/chat/conversations/${conversationId}/messages`, { text });
  }

  deleteConversation(id: number): Observable<void> {
    return this.http.delete<void>(`/api/chat/conversations/${id}`);
  }
}
