import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { DealService } from '../../../services/deal.service';
import { DealDto } from '../../../models/deal.model';

@Component({
  selector: 'app-deal-list',
  standalone: false,
  templateUrl: './deal-list.html',
  styleUrl: './deal-list.scss'
})
export class DealList implements OnInit {
  deals: DealDto[] = [];
  displayedColumns: string[] = ['name', 'companyId', 'amount', 'currency', 'probability', 'expectedCloseDate'];

  constructor(
    private dealService: DealService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadDeals();
  }

  loadDeals(): void {
    this.dealService.getAll().subscribe({
      next: (deals) => {
        console.log('Deals loaded successfully:', deals);
        this.deals = deals;
        console.log('Deals array length:', this.deals.length);
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading deals:', error);
      }
    });
  }

  createNew(): void {
    this.router.navigate(['/deals/new']);
  }
}