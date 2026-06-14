import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CommentDto {
  id: number;
  workItemId: number;
  authorId: number;
  authorName: string;
  text: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class CommentsService {
  private readonly http = inject(HttpClient);

  getComments(projectId: number, workItemId: number): Observable<CommentDto[]> {
    return this.http.get<CommentDto[]>(`/api/projects/${projectId}/workitems/${workItemId}/comments`);
  }

  createComment(projectId: number, workItemId: number, text: string): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`/api/projects/${projectId}/workitems/${workItemId}/comments`, { text });
  }

  deleteComment(projectId: number, workItemId: number, commentId: number): Observable<void> {
    return this.http.delete<void>(`/api/projects/${projectId}/workitems/${workItemId}/comments/${commentId}`);
  }
}
