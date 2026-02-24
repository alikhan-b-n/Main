import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { CompanyService } from '../../../services/company.service';
import { CompanyDto } from '../../../models/company.model';

@Component({
  selector: 'app-company-list',
  standalone: false,
  templateUrl: './company-list.html',
  styleUrl: './company-list.scss'
})
export class CompanyList implements OnInit {
  companies: CompanyDto[] = [];
  displayedColumns: string[] = ['name', 'domain', 'industry', 'website', 'totalSpent', 'createdAt'];

  constructor(
    private companyService: CompanyService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCompanies();
  }

  loadCompanies(): void {
    this.companyService.getAll().subscribe({
      next: (companies) => {
        console.log('Companies loaded successfully:', companies);
        this.companies = companies;
        console.log('Companies array length:', this.companies.length);
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading companies:', error);
      }
    });
  }

  createNew(): void {
    this.router.navigate(['/companies/new']);
  }
}