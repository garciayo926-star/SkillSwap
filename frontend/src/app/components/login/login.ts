import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent {

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

  errorMessage = '';
  successMessage = '';

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  activarLampara(): void {
    this.lamparaEncendida = !this.lamparaEncendida;

    this.errorMessage = '';
    this.successMessage = '';
  }

  toggleMode(loginMode: boolean): void {
    this.isLoginMode = loginMode;

    this.errorMessage = '';
    this.successMessage = '';
  }

 onLogin(): void {
    console.log("Intentando iniciar sesión con:", this.loginEmail);

    this.authService.login({
      email: this.loginEmail,
      password: this.loginPassword
    }).subscribe({
      next: (response) => {
        console.log("Respuesta completa del backend:", response);

        // Verificamos si los datos existen antes de guardarlos
        if (response && response.studentId) {
          localStorage.setItem('studentId', response.studentId);
          localStorage.setItem('username', response.username);
          
          this.successMessage = '¡Inicio de sesión exitoso!';
          console.log("Navegando al dashboard...");
          
          this.router.navigate(['/dashboard']).then(success => {
            console.log("¿Navegación exitosa?:", success);
          });
        } else {
          console.warn("La respuesta del backend no trajo studentId:", response);
        }
      },
      error: (err) => {
        console.error("Error en la petición de login:", err);
        this.errorMessage =
          err.error?.message ||
          'Correo o contraseña incorrectos.';
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
        this.successMessage =
          '¡Registro exitoso! Ya puedes iniciar sesión.';

        this.toggleMode(true);
      },
      error: (err) => {
        this.errorMessage =
          err.error?.message ||
          'Error al registrar el usuario.';
      }
    });
  }
}