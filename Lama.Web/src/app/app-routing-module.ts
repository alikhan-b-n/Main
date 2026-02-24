import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { Dashboard } from './components/dashboard/dashboard';
import { ContactList } from './components/contacts/contact-list/contact-list';
import { ContactForm } from './components/contacts/contact-form/contact-form';
import { CompanyList } from './components/companies/company-list/company-list';
import { CompanyForm } from './components/companies/company-form/company-form';
import { DealList } from './components/deals/deal-list/deal-list';
import { DealForm } from './components/deals/deal-form/deal-form';
import { TicketList } from './components/tickets/ticket-list/ticket-list';
import { TicketForm } from './components/tickets/ticket-form/ticket-form';

// AiTools is a standalone component and will be lazy-loaded via loadComponent to keep it out of the initial bundle

const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: Dashboard },
  { path: 'ai', loadComponent: () => import('./components/ai/ai-tools/ai-tools').then(m => m.AiTools) },
  { path: 'contacts', component: ContactList },
  { path: 'contacts/new', component: ContactForm },
  { path: 'companies', component: CompanyList },
  { path: 'companies/new', component: CompanyForm },
  { path: 'deals', component: DealList },
  { path: 'deals/new', component: DealForm },
  { path: 'tickets', component: TicketList },
  { path: 'tickets/new', component: TicketForm }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
