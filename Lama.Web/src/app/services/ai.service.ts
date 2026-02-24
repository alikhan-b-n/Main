import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ActivitySummaryDto } from '../models/ai.model';

@Injectable({
  providedIn: 'root'
})
export class AiService {
  private baseUrl = `${environment.apiUrl}/crm/ai/activities`;

  constructor(private http: HttpClient) {}

  summarizeActivity(activityId: string): Observable<ActivitySummaryDto> {
    return this.http.post<ActivitySummaryDto>(`${this.baseUrl}/${activityId}/summarize`, {});
  }
}

