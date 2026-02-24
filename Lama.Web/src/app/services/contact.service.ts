import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ContactDto, CreateContactCommand } from '../models/contact.model';

@Injectable({
  providedIn: 'root'
})
export class ContactService {
  private apiUrl = `${environment.apiUrl}/crm/objects/contacts`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ContactDto[]> {
    return this.http.get<ContactDto[]>(this.apiUrl);
  }

  getById(id: string): Observable<ContactDto> {
    return this.http.get<ContactDto>(`${this.apiUrl}/${id}`);
  }

  create(command: CreateContactCommand): Observable<string> {
    return this.http.post<string>(this.apiUrl, command);
  }
}
