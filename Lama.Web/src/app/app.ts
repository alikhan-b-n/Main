import { Component } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.scss'
})
export class App {
  title = 'Lama CRM';
  currentPageTitle = 'Dashboard';

  constructor(private router: Router) {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.updatePageTitle();
      });
  }

  private updatePageTitle() {
    const route = this.router.url.split('/')[1] || 'dashboard';
    this.currentPageTitle = this.formatTitle(route);
  }

  private formatTitle(route: string): string {
    const titles: { [key: string]: string } = {
      'dashboard': 'Dashboard',
      'companies': 'Companies',
      'contacts': 'Contacts',
      'deals': 'Deals',
      'tickets': 'Tickets',
      'ai': 'AI Tools'
    };
    return titles[route] || 'Lama CRM';
  }

  getPageTitle(): string {
    return this.currentPageTitle;
  }
}
