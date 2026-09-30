import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import AuthLayout from "../components/auth/AuthLayout";
import FormField from "../components/auth/FormField";
import SubmitButton from "../components/auth/SubmitButton";
import { register, type RegisterRequest } from "../features/auth/authService";

function RegisterPage() {
  const navigate = useNavigate();
  const [values, setValues] = useState<RegisterRequest>({
    FirstName: "",
    LastName: "",
    Email: "",
    Password: "",
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      await register(values);
      navigate("/login", { state: { registrationSucceeded: true } });
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to register. Please try again.",
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthLayout mode="register">
      <div className="mb-7">
        <p className="text-xs font-bold uppercase tracking-[0.14em] text-[#758682]">
          Get started
        </p>
        <h2 className="mt-2 text-2xl font-semibold text-[#172b2a]">
          Create your account
        </h2>
      </div>
      <form className="space-y-5" onSubmit={handleSubmit}>
        <div className="grid gap-5 sm:grid-cols-2">
          <FormField
            id="firstName"
            label="First name"
            autoComplete="given-name"
            value={values.FirstName}
            onChange={(FirstName) => setValues({ ...values, FirstName })}
          />
          <FormField
            id="lastName"
            label="Last name"
            autoComplete="family-name"
            value={values.LastName}
            onChange={(LastName) => setValues({ ...values, LastName })}
          />
        </div>
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
          autoComplete="new-password"
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
        <SubmitButton busy={busy}>Create account</SubmitButton>
      </form>
      <p className="mt-6 text-center text-sm text-[#586b68]">
        Already have an account?{" "}
        <Link
          className="font-bold text-[#176b61] underline decoration-[#e6a23c] decoration-2 underline-offset-4"
          to="/login"
        >
          Log in
        </Link>
      </p>
    </AuthLayout>
  );
}

export default RegisterPage;
