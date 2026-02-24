import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { TicketService } from '../../../services/ticket.service';
import { TicketDto } from '../../../models/ticket.model';

@Component({
  selector: 'app-ticket-list',
  standalone: false,
  templateUrl: './ticket-list.html',
  styleUrl: './ticket-list.scss'
})
export class TicketList implements OnInit {
  tickets: TicketDto[] = [];
  displayedColumns: string[] = ['ticketName', 'status', 'priority', 'source', 'contactId', 'createdAt'];

  constructor(
    private ticketService: TicketService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadTickets();
  }

  loadTickets(): void {
    this.ticketService.getAll().subscribe({
      next: (tickets) => {
        console.log('Tickets loaded successfully:', tickets);
        this.tickets = tickets;
        console.log('Tickets array length:', this.tickets.length);
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading tickets:', error);
      }
    });
  }

  createNew(): void {
    this.router.navigate(['/tickets/new']);
  }

  getStatusBadgeClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'open': return 'bg-primary';
      case 'in_progress': return 'bg-warning';
      case 'resolved': return 'bg-success';
      case 'closed': return 'bg-secondary';
      default: return 'bg-secondary';
    }
  }

  getPriorityBadgeClass(priority: string): string {
    switch (priority.toLowerCase()) {
      case 'urgent': return 'bg-danger';
      case 'high': return 'bg-warning';
      case 'medium': return 'bg-info';
      case 'low': return 'bg-secondary';
      default: return 'bg-secondary';
    }
  }
}