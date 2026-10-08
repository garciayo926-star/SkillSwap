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

    // Validación del lado del cliente antes de llamar a la API
    const email = this.loginEmail.trim();
    if (!email || !this.loginPassword) {
      this.errorMessage.set('Ingresa tu correo electrónico y tu contraseña.');
      return;
    }
    // Formato de correo básico
    const emailOk = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    if (!emailOk) {
      this.errorMessage.set('El correo electrónico no tiene un formato válido.');
      return;
    }

    this.authService.login({
      email,
      password: this.loginPassword
    }).subscribe({
      next: (response) => {
        // AuthService ya guardó token, userId, username, userRole y studentId en localStorage.
        // Administrador/Moderador pueden no tener perfil de estudiante, por eso se valida el token.
        if (response && response.token) {
          this.successMessage.set('¡Inicio de sesión exitoso!');
          this.router.navigate(['/dashboard']);
        } else {
          this.errorMessage.set('Respuesta inesperada del servidor.');
        }
      },
      error: (err) => {
        // 401 -> credenciales incorrectas; 0 -> API apagada; otros -> mensaje del backend
        if (err.status === 0) {
          this.errorMessage.set('No se pudo conectar con el servidor. Verifica que la API esté encendida.');
        } else {
          this.errorMessage.set(err.error?.message || 'Correo o contraseña incorrectos.');
        }
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