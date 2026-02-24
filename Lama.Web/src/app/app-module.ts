import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

// ng-bootstrap
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { Dashboard } from './components/dashboard/dashboard';
import { ContactList } from './components/contacts/contact-list/contact-list';
import { ContactForm } from './components/contacts/contact-form/contact-form';
import { CompanyList } from './components/companies/company-list/company-list';
import { CompanyForm } from './components/companies/company-form/company-form';
import { DealList } from './components/deals/deal-list/deal-list';
import { DealForm } from './components/deals/deal-form/deal-form';
import { TicketList } from './components/tickets/ticket-list/ticket-list';
import { TicketForm } from './components/tickets/ticket-form/ticket-form';
import { ToastsComponent } from './components/toasts/toasts.component';

// Interceptors
import { errorHandlerInterceptor } from './services/error-handler.interceptor';

@NgModule({
  declarations: [
    App,
    Dashboard,
    ContactList,
    ContactForm,
    CompanyList,
    CompanyForm,
    DealList,
    DealForm,
    TicketList,
    TicketForm,
    ToastsComponent
  ],
  imports: [
    BrowserModule,
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    AppRoutingModule,
    NgbModule
  ],
  providers: [
    provideHttpClient(
      withInterceptors([errorHandlerInterceptor])
    )
  ],
  bootstrap: [App]
})
export class AppModule { }
