import type { ReactNode } from "react";
import { Link } from "react-router-dom";

type AuthLayoutProps = {
  children: ReactNode;
  mode: "login" | "register";
};

function AuthLayout({ children, mode }: AuthLayoutProps) {
  const isRegister = mode === "register";

  return (
    <main className="min-h-screen overflow-hidden">
      <header className="border-b border-[#dce6e3] bg-white/85">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-5">
          <Link
            className="flex items-center gap-3 text-sm font-bold tracking-wide text-[#172b2a]"
            to="/login"
          >
            <span className="grid size-9 place-items-center rounded-lg bg-[#176b61] text-white">
              M
            </span>
            MICRO / SERVICES
          </Link>
          <nav
            aria-label="Authentication"
            className="flex items-center gap-1 rounded-lg bg-[#eaf1ee] p-1 text-sm font-semibold"
          >
            <Link
              className={`rounded-md px-3 py-2 ${!isRegister ? "bg-white text-[#176b61] shadow-sm" : "text-[#586b68]"}`}
              to="/login"
            >
              Log in
            </Link>
            <Link
              className={`rounded-md px-3 py-2 ${isRegister ? "bg-white text-[#176b61] shadow-sm" : "text-[#586b68]"}`}
              to="/register"
            >
              Register
            </Link>
          </nav>
        </div>
      </header>
      <section className="mx-auto grid max-w-6xl gap-12 px-6 py-12 md:grid-cols-[0.9fr_1.1fr] md:items-center md:gap-20 md:py-20">
        <div className="animate-[rise-in_500ms_ease-out_both]">
          <p className="mb-5 text-xs font-bold uppercase tracking-[0.16em] text-[#176b61]">
            Microservices account
          </p>
          <h1 className="max-w-lg text-4xl font-semibold leading-tight text-[#172b2a] sm:text-5xl">
            {isRegister
              ? "Make room for what’s next."
              : "Good to have you back."}
          </h1>
          <p className="mt-6 max-w-md text-base leading-7 text-[#586b68]">
            {isRegister
              ? "Create an account to get started with the service workspace."
              : "Log in to continue to your service workspace."}
          </p>
          <div className="mt-10 flex items-center gap-3 border-t border-[#dce6e3] pt-5 text-sm text-[#586b68]">
            <span className="size-2 rounded-full bg-[#e6a23c]" />
            Secure access through the shared API gateway
          </div>
        </div>
        <div className="animate-[rise-in_600ms_100ms_ease-out_both] border-t-2 border-[#e6a23c] bg-white px-6 py-7 shadow-[0_18px_50px_-38px_rgba(23,43,42,0.4)] sm:px-9 sm:py-9">
          {children}
        </div>
      </section>
    </main>
  );
}

export default AuthLayout;
