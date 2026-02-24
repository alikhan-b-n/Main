# Lama CRM - Angular Frontend

This is the Angular frontend for the Lama CRM application, a comprehensive Customer Relationship Management system built with Angular 21 and Angular Material.

## Features

The frontend provides complete UI for managing:

- **Accounts**: Manage customer accounts, prospects, partners, and competitors
- **Contacts**: Track contacts and their relationships with accounts
- **Opportunities**: Manage sales opportunities and track deals through the pipeline
- **Support Cases**: Track and manage customer support cases and interactions

## Tech Stack

- **Angular 21**: Modern web framework
- **Angular Material**: UI component library with Material Design
- **TypeScript**: Type-safe JavaScript
- **SCSS**: Styling with Sass preprocessor
- **RxJS**: Reactive programming with observables

## Prerequisites

- Node.js (v20.19.0, v22.12.0, or v24.0.0+)
- npm (v8.0.0+)
- The Lama CRM backend API running on `http://localhost:5000`

## Installation

1. Navigate to the frontend directory:
   ```bash
   cd Lama.Web
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

## Configuration

The API base URL is configured in the environment files:

- **Development**: `src/environments/environment.ts` - Points to `http://localhost:5000/api`
- **Production**: `src/environments/environment.prod.ts` - Points to `/api`

You can modify these files to point to a different API endpoint if needed.

## Running the Application

### Development Server

Start the development server:

```bash
npm start
```

Or:

```bash
npx ng serve
```

The application will be available at `http://localhost:4200`

The app will automatically reload when you make changes to the source files.

### Production Build

Build the application for production:

```bash
npx ng build --configuration production
```

The build artifacts will be stored in the `dist/` directory.

## Project Structure

```
Lama.Web/
├── src/
│   ├── app/
│   │   ├── components/          # UI Components
│   │   │   ├── dashboard/       # Main dashboard with navigation
│   │   │   ├── accounts/        # Account list and form
│   │   │   ├── contacts/        # Contact list and form
│   │   │   ├── opportunities/   # Opportunity list and form
│   │   │   └── support-cases/   # Support case list and form
│   │   ├── models/              # TypeScript interfaces and DTOs
│   │   │   ├── account.model.ts
│   │   │   ├── contact.model.ts
│   │   │   ├── opportunity.model.ts
│   │   │   ├── support-case.model.ts
│   │   │   └── organization.model.ts
│   │   ├── services/            # API Services
│   │   │   ├── account.service.ts
│   │   │   ├── contact.service.ts
│   │   │   ├── opportunity.service.ts
│   │   │   ├── support-case.service.ts
│   │   │   └── organization.service.ts
│   │   ├── app.ts               # Root component
│   │   ├── app.html             # Root template
│   │   ├── app.scss             # Root styles
│   │   ├── app-module.ts        # Root module
│   │   └── app-routing-module.ts # Routing configuration
│   ├── environments/            # Environment configurations
│   ├── index.html               # Main HTML file
│   ├── main.ts                  # Application entry point
│   └── styles.scss              # Global styles
├── angular.json                 # Angular CLI configuration
├── package.json                 # npm dependencies
└── tsconfig.json                # TypeScript configuration
```

## Key Features

### Dashboard

The dashboard provides a central hub with:
- Navigation sidebar with all main sections
- Quick access cards for each module
- Material Design UI components

### Accounts Management

- List all accounts with filtering and sorting
- Create new accounts (Prospect, Customer, Partner, Competitor)
- View account details including industry, website, and contact information

### Contacts Management

- List all contacts with their details
- Create new contacts with email, phone, job title
- Associate contacts with accounts

### Opportunities Management

- Track sales opportunities through the sales pipeline
- View opportunity stage, probability, and expected revenue
- Set expected close dates
- Support for multiple currencies

### Support Cases Management

- Track customer support cases
- Set priority levels (Low, Medium, High, Critical)
- Monitor case status (Open, In Progress, Resolved, Closed)
- Associate cases with accounts and contacts

## API Integration

All services use the Angular HttpClient to communicate with the backend REST API:

- **GET** requests retrieve data (lists and individual records)
- **POST** requests create new records
- Error handling with snackbar notifications
- Reactive programming with RxJS observables

## Angular Material Components

The application uses the following Material components:

- **Layout**: Sidenav, Toolbar, Card
- **Navigation**: List, Button, Icon
- **Forms**: Form Field, Input, Select, Datepicker
- **Data Display**: Table
- **Feedback**: Snackbar

## Styling

- Uses Angular Material's Indigo-Pink theme
- SCSS for component-specific styles
- Responsive layout with flexbox
- Material Design principles

## Development

### Adding New Features

1. Generate a new component:
   ```bash
   npx ng generate component components/feature-name
   ```

2. Create the corresponding service:
   ```bash
   npx ng generate service services/feature-name
   ```

3. Add the route in `app-routing-module.ts`

4. Update the dashboard navigation if needed

### Code Quality

- TypeScript strict mode enabled
- Component-based architecture
- Reactive forms for form handling
- Service layer for API communication
- Type-safe models and interfaces

## Troubleshooting

### CORS Issues

If you encounter CORS issues, ensure the backend API has CORS configured to allow requests from `http://localhost:4200`

### API Connection Issues

- Verify the backend API is running on `http://localhost:5000`
- Check the environment configuration in `src/environments/environment.ts`
- Open browser developer tools to check for network errors

### Module Issues

If you see module not found errors:
```bash
npm install
```

## Browser Support

The application supports modern browsers:
- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

## License

This project is part of the Lama CRM system.
