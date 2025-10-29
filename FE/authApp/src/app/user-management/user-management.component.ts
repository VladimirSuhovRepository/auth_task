import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService, UserModel, NotificationService, UsersResponse, User, AuthService, CreateUserRequest } from '../../services';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.scss'
})
export class UserManagementComponent implements OnInit {
  users: UserModel[] = [];
  loading = false;
  searchTerm = '';
  selectedUser: UserModel | null = null;
  loggedInUser: User | null = null;

  // Create user modal
  showCreateUserModal = false;
  createUserForm!: FormGroup;
  isCreatingUser = false;

  // Edit user modal
  showEditUserModal = false;
  editUserForm!: FormGroup;
  isUpdatingUser = false;
  userToEdit: UserModel | null = null;
  showEditPassword = false;

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private notificationService: NotificationService,
    private fb: FormBuilder,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.loggedInUser = this.authService.getCurrentUser() || null;
    this.initializeCreateUserForm();
    this.initializeEditUserForm();
  }

  private initializeCreateUserForm(): void {
    this.createUserForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      roles: ['', [Validators.required]] // Required role selection
    });
  }

  private initializeEditUserForm(): void {
    this.editUserForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      firstName: [''],
      lastName: [''],
      roles: ['', [Validators.required]], // Required role selection
      isActive: [true]
    });
  }

  loadUsers(): void {
    this.loading = true;
    this.userService.getAllUsers({
      search: this.searchTerm,
      page: 1,
      limit: 50
    }).subscribe({
      next: (response: User[]) => {
        this.users = response;
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Error loading users:', error);
        this.notificationService.error('Error', 'Failed to load users');
        this.loading = false;
      }
    });
  }

  selectUser(user: UserModel): void {
    this.selectedUser = user;
  }

  deleteUser(userId: string): void {
    if (confirm('Are you sure you want to delete this user?')) {
      this.userService.deleteUser(userId).subscribe({
        next: () => {
          this.notificationService.success('Success', 'User deleted successfully');
          this.loadUsers();
        },
        error: (error: any) => {
          console.error('Error deleting user:', error);
          this.notificationService.error('Error', 'Failed to delete user');
        }
      });
    }
  }

  // Create User Methods
  openCreateUserModal(): void {
    this.showCreateUserModal = true;
    this.createUserForm.reset();
    // No default role - user must select one
  }

  closeCreateUserModal(): void {
    this.showCreateUserModal = false;
    this.createUserForm.reset();
  }

  onCreateUser(): void {
    if (this.createUserForm.invalid) {
      this.markFormGroupTouched(this.createUserForm);
      return;
    }

    this.isCreatingUser = true;
    const formValue = this.createUserForm.value;

    const newUser: CreateUserRequest = {
      username: formValue.email, // Use email as username
      email: formValue.email.trim(),
      password: formValue.password,
      roles: [formValue.roles] // Convert to array as expected by the interface
    };

    this.userService.createUser(newUser).subscribe({
      next: (createdUser) => {
        this.isCreatingUser = false;
        this.notificationService.success(
          'User Created',
          `User ${createdUser.email} has been created successfully!`
        );
        this.closeCreateUserModal();
        this.loadUsers(); // Refresh the user list
      },
      error: (error: any) => {
        this.isCreatingUser = false;
        console.error('Error creating user:', error);

        let errorMessage = 'Failed to create user. Please try again.';
        if (error.error?.message) {
          errorMessage = error.error.message;
        } else if (error.message) {
          errorMessage = error.message;
        }

        this.notificationService.error('Creation Failed', errorMessage);
      }
    });
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  // Edit User Methods
  openEditUserModal(user: UserModel): void {
    this.userToEdit = user;
    this.showEditUserModal = true;

    // Populate form with current user data, password field left empty (required)
    this.editUserForm.patchValue({
      id: user.id,
      email: user.email,
      password: '', // Leave empty to require new password
      firstName: user.firstName || '',
      lastName: user.lastName || '',
      roles: user.roles?.[0] || 'user',
      isActive: user.isActive !== false
    });
  }

  closeEditUserModal(): void {
    this.showEditUserModal = false;
    this.userToEdit = null;
    this.editUserForm.reset();
  }

  onUpdateUser(): void {
    if (!this.userToEdit || this.editUserForm.invalid) {
      this.markFormGroupTouched(this.editUserForm);
      return;
    }

    this.isUpdatingUser = true;
    const formValue = this.editUserForm.value;

    const updateData: any = {
      id: this.userToEdit.id,
      email: formValue.email.trim(),
      password: formValue.password.trim(), // Password is now required
      firstName: formValue.firstName?.trim(),
      lastName: formValue.lastName?.trim(),
      roles: [formValue.roles],
      isActive: formValue.isActive
    };

    this.userService.updateUser(this.userToEdit.id, updateData).subscribe({
      next: (updatedUser) => {
        this.isUpdatingUser = false;
        this.notificationService.success(
          'User Updated',
          `User ${updateData.email} has been updated successfully!`
        );
        this.closeEditUserModal();
        this.loadUsers(); // Refresh the user list
      },
      error: (error: any) => {
        this.isUpdatingUser = false;
        console.error('Error updating user:', error);

        let errorMessage = 'Failed to update user. Please try again.';
        if (error.error?.message) {
          errorMessage = error.error.message;
        } else if (error.message) {
          errorMessage = error.message;
        }

        this.notificationService.error('Update Failed', errorMessage);
      }
    });
  }

  // Form validation getters
  get email() { return this.createUserForm.get('email'); }
  get password() { return this.createUserForm.get('password'); }
  get firstName() { return this.createUserForm.get('firstName'); }
  get lastName() { return this.createUserForm.get('lastName'); }
  get roles() { return this.createUserForm.get('roles'); }

  get isEmailInvalid(): boolean {
    return !!(this.email?.invalid && this.email?.touched);
  }

  get isPasswordInvalid(): boolean {
    return !!(this.password?.invalid && this.password?.touched);
  }

  get isFirstNameInvalid(): boolean {
    return !!(this.firstName?.invalid && this.firstName?.touched);
  }

  get isLastNameInvalid(): boolean {
    return !!(this.lastName?.invalid && this.lastName?.touched);
  }

  get isRolesInvalid(): boolean {
    return !!(this.roles?.invalid && this.roles?.touched);
  }

  // Edit form validation getters
  get editEmail() { return this.editUserForm.get('email'); }
  get editPassword() { return this.editUserForm.get('password'); }
  get editFirstName() { return this.editUserForm.get('firstName'); }
  get editLastName() { return this.editUserForm.get('lastName'); }
  get editRoles() { return this.editUserForm.get('roles'); }

  get isEditEmailInvalid(): boolean {
    return !!(this.editEmail?.invalid && this.editEmail?.touched);
  }

  get isEditPasswordInvalid(): boolean {
    return !!(this.editPassword?.invalid && this.editPassword?.touched);
  }

  get isEditFirstNameInvalid(): boolean {
    return !!(this.editFirstName?.invalid && this.editFirstName?.touched);
  }

  get isEditLastNameInvalid(): boolean {
    return !!(this.editLastName?.invalid && this.editLastName?.touched);
  }

  get isEditRolesInvalid(): boolean {
    return !!(this.editRoles?.invalid);
  }

  // Password visibility toggle for edit form
  toggleEditPasswordVisibility(): void {
    this.showEditPassword = !this.showEditPassword;
  }

  // Logout functionality
  onLogout(): void {
    // Clear local storage manually since logout method doesn't exist
    localStorage.removeItem('authToken');
    localStorage.removeItem('currentUser');
    this.notificationService.success('Logged Out', 'You have been logged out successfully');
    this.router.navigate(['/login']);
  }
}
