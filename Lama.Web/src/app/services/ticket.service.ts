import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { TicketDto, CreateTicketCommand } from '../models/ticket.model';

@Injectable({
  providedIn: 'root'
})
export class TicketService {
  private apiUrl = `${environment.apiUrl}/crm/objects/tickets`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<TicketDto[]> {
    return this.http.get<TicketDto[]>(this.apiUrl);
  }

  getById(id: string): Observable<TicketDto> {
    return this.http.get<TicketDto>(`${this.apiUrl}/${id}`);
  }

  create(command: CreateTicketCommand): Observable<string> {
    return this.http.post<string>(this.apiUrl, command);
  }
}
