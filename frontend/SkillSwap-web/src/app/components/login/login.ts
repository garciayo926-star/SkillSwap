import { Component, ChangeDetectionStrategy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent implements OnInit {

  // Control de la lámpara
  lamparaEncendida = false;

  isLoginMode = true;

  loginEmail = '';
  loginPassword = '';

  regUsername = '';
  regEmail = '';
  regPassword = '';
  regFirstName = '';
  regLastName = '';
  regBio = '';

  // Los mensajes se asignan dentro de respuestas HTTP (asíncronas): signals para refrescar la vista
  errorMessage = signal('');
  successMessage = signal('');

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Si ya existe una sesión activa, se envía directamente al dashboard
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  activarLampara(): void {
    this.lamparaEncendida = !this.lamparaEncendida;

    this.errorMessage.set('');
    this.successMessage.set('');
  }

  toggleMode(loginMode: boolean): void {
    this.isLoginMode = loginMode;

    this.errorMessage.set('');
    this.successMessage.set('');
  }

  onLogin(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    this.authService.login({
      email: this.loginEmail,
      password: this.loginPassword
    }).subscribe({
      next: (response) => {
        // AuthService ya guardó userId, username, userRole y studentId en localStorage.
        // Master/Technical pueden no tener perfil de estudiante, por eso se valida userId.
        if (response && response.userId) {
          this.successMessage.set('¡Inicio de sesión exitoso!');
          this.router.navigate(['/dashboard']);
        } else {
          this.errorMessage.set('Respuesta inesperada del servidor.');
        }
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Correo o contraseña incorrectos.');
      }
    });
  }

  onRegister(): void {
    const newUser = {
      username: this.regUsername,
      email: this.regEmail,
      password: this.regPassword,
      firstName: this.regFirstName,
      lastName: this.regLastName,
      bio: this.regBio
    };

    this.authService.register(newUser).subscribe({
      next: () => {
        // toggleMode limpia los mensajes, por eso el mensaje de éxito se asigna después
        this.toggleMode(true);
        this.loginEmail = this.regEmail;

        this.successMessage.set('¡Registro exitoso! Ya puedes iniciar sesión.');
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Error al registrar el usuario.');
      }
    });
  }
}