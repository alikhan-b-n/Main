import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { ContactService } from '../../../services/contact.service';
import { ContactDto, CreateContactCommand } from '../../../models/contact.model';

@Component({
  selector: 'app-contact-list',
  standalone: false,
  templateUrl: './contact-list.html',
  styleUrl: './contact-list.scss'
})
export class ContactList implements OnInit {
  contacts: ContactDto[] = [];
  displayedColumns: string[] = ['firstName', 'lastName', 'email', 'phoneNumber', 'jobTitle', 'createdAt'];

  constructor(
    private contactService: ContactService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadContacts();
  }

  loadContacts(): void {
    this.contactService.getAll().subscribe({
      next: (contacts) => {
        console.log('Contacts loaded successfully:', contacts);
        this.contacts = contacts;
        console.log('Contacts array length:', this.contacts.length);
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading contacts:', error);
      }
    });
  }

  createNew(): void {
    this.router.navigate(['/contacts/new']);
  }
}
