import { useState, type FormEvent } from "react";
import { Link, useLocation } from "react-router-dom";
import AuthLayout from "../components/auth/AuthLayout";
import FormField from "../components/auth/FormField";
import RegistrationSuccessDialog from "../components/auth/RegistrationSuccessDialog";
import SubmitButton from "../components/auth/SubmitButton";
import { login, type LoginRequest } from "../features/auth/authService";

function LoginPage() {
  const location = useLocation();
  const registrationSucceeded =
    (location.state as { registrationSucceeded?: boolean } | null)
      ?.registrationSucceeded ?? false;
  const [values, setValues] = useState<LoginRequest>({
    Email: "",
    Password: "",
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError("");
    setMessage("");
    try {
      const response = await login(values);
      setMessage(response.message || "Login successful.");
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to log in. Please try again.",
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthLayout mode="login">
      <div className="mb-7">
        <p className="text-xs font-bold uppercase tracking-[0.14em] text-[#758682]">
          Welcome back
        </p>
        <h2 className="mt-2 text-2xl font-semibold text-[#172b2a]">
          Log in to your account
        </h2>
      </div>
      <form className="space-y-5" onSubmit={handleSubmit}>
        <FormField
          id="email"
          label="Email address"
          type="email"
          autoComplete="email"
          value={values.Email}
          onChange={(Email) => setValues({ ...values, Email })}
        />
        <FormField
          id="password"
          label="Password"
          type="password"
          autoComplete="current-password"
          value={values.Password}
          onChange={(Password) => setValues({ ...values, Password })}
        />
        {error && (
          <p
            className="rounded-md bg-[#fff2e8] px-3 py-2 text-sm text-[#8a4a22]"
            role="alert"
          >
            {error}
          </p>
        )}
        {message && (
          <p
            className="rounded-md bg-[#edf7f1] px-3 py-2 text-sm text-[#176b61]"
            role="status"
          >
            {message}
          </p>
        )}
        <SubmitButton busy={busy}>Log in</SubmitButton>
      </form>
      <p className="mt-6 text-center text-sm text-[#586b68]">
        New here?{" "}
        <Link
          className="font-bold text-[#176b61] underline decoration-[#e6a23c] decoration-2 underline-offset-4"
          to="/register"
        >
          Create an account
        </Link>
      </p>
      {registrationSucceeded && <RegistrationSuccessDialog />}
    </AuthLayout>
  );
}

export default LoginPage;
