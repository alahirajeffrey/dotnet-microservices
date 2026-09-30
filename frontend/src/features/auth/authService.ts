import { apiRequest } from "../../lib/api";

export type LoginRequest = {
  Email: string;
  Password: string;
};

export type RegisterRequest = LoginRequest & {
  FirstName: string;
  LastName: string;
};

export type AuthResponse = {
  status?: string;
  message?: string;
};

export function login(request: LoginRequest) {
  return apiRequest<AuthResponse>("/api/v1/Auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
}

export function register(request: RegisterRequest) {
  return apiRequest<AuthResponse>("/api/v1/Auth/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
}
