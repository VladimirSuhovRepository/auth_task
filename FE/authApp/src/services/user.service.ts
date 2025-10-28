import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface User {
  id: string;
  username: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  roles?: string[];
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
  lastLogin?: string;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  roles?: string[];
}

export interface UpdateUserRequest {
  username?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  roles?: string[];
  isActive?: boolean;
}

export interface UsersResponse {
  users: User[];
  total: number;
  page: number;
  limit: number;
  totalPages: number;
}

export interface UserQueryParams {
  page?: number;
  limit?: number;
  search?: string;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  role?: string;
  isActive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  constructor(private apiService: ApiService) {}

  getAllUsers(params?: UserQueryParams): Observable<User[]> {
    let endpoint = '/api/users';

    if (params) {
      const queryParams = new URLSearchParams();
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, value.toString());
        }
      });

      if (queryParams.toString()) {
        endpoint += `?${queryParams.toString()}`;
      }
    }

    return this.apiService.get<User[]>(endpoint);
  }

  getUserById(id: string): Observable<User> {
    return this.apiService.get<User>(`/api/users/${id}`);
  }

  createUser(userData: CreateUserRequest): Observable<User> {
    return this.apiService.post<User>('/api/users', userData);
  }

  updateUser(id: string, userData: UpdateUserRequest): Observable<User> {
    return this.apiService.put<User>(`/api/users/${id}`, userData);
  }

  deleteUser(id: string): Observable<any> {
    return this.apiService.delete(`/api/users/${id}`);
  }

  activateUser(id: string): Observable<User> {
    return this.apiService.patch<User>(`/api/users/${id}/activate`, {});
  }

  deactivateUser(id: string): Observable<User> {
    return this.apiService.patch<User>(`/api/users/${id}/deactivate`, {});
  }

  assignRole(userId: string, role: string): Observable<User> {
    return this.apiService.patch<User>(`/api/users/${userId}/roles`, { role, action: 'add' });
  }

  removeRole(userId: string, role: string): Observable<User> {
    return this.apiService.patch<User>(`/api/users/${userId}/roles`, { role, action: 'remove' });
  }

  searchUsers(query: string): Observable<User[]> {
    return this.apiService.get<User[]>(`/api/users/search?q=${encodeURIComponent(query)}`);
  }

  getUserProfile(): Observable<User> {
    return this.apiService.get<User>('/api/users/profile');
  }

  updateProfile(userData: UpdateUserRequest): Observable<User> {
    return this.apiService.put<User>('/api/users/profile', userData);
  }

  uploadAvatar(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('avatar', file);

    // Note: This might need a different HTTP call if your API expects multipart/form-data
    return this.apiService.post('/api/users/avatar', formData);
  }

  getUserStats(): Observable<any> {
    return this.apiService.get('/api/users/stats');
  }

  exportUsers(format: 'csv' | 'xlsx' = 'csv'): Observable<Blob> {
    return this.apiService.get(`/api/users/export?format=${format}`);
  }

  bulkUpdateUsers(userIds: string[], updateData: UpdateUserRequest): Observable<any> {
    return this.apiService.patch('/api/users/bulk-update', {
      userIds,
      updateData
    });
  }

  bulkDeleteUsers(userIds: string[]): Observable<any> {
    return this.apiService.delete('/api/users/bulk-delete').pipe(
      // Note: You might need to send userIds in the request body
    );
  }
}