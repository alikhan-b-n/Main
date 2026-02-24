import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CompanyDto, CreateCompanyCommand } from '../models/company.model';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {
  private apiUrl = `${environment.apiUrl}/crm/objects/companies`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<CompanyDto[]> {
    return this.http.get<CompanyDto[]>(this.apiUrl);
  }

  getById(id: string): Observable<CompanyDto> {
    return this.http.get<CompanyDto>(`${this.apiUrl}/${id}`);
  }

  create(command: CreateCompanyCommand): Observable<string> {
    return this.http.post<string>(this.apiUrl, command);
  }
}
