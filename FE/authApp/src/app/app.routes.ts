import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { UserManagementComponent } from './user-management/user-management.component';
import { AuthGuard } from '../services';

export const routes: Routes = [
    { path: '', redirectTo: '/login', pathMatch: 'full' },
    { path: 'login', component: LoginComponent },
    {
        path: 'user-management',
        component: UserManagementComponent,
        canActivate: [AuthGuard]
    },
    { path: 'register', redirectTo: '/login' }, // Placeholder until register component is created
    { path: '**', redirectTo: '/login' } // Wildcard route for 404 cases
];