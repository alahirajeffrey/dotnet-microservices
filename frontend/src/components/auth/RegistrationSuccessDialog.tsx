import { useNavigate } from "react-router-dom";

function RegistrationSuccessDialog() {
  const navigate = useNavigate();

  return (
    <div
      className="fixed inset-0 z-10 grid place-items-center bg-[#172b2a]/45 px-5"
      role="presentation"
    >
      <section
        aria-labelledby="registration-success-title"
        aria-modal="true"
        className="w-full max-w-md border-t-2 border-[#e6a23c] bg-white p-7 shadow-2xl"
        role="dialog"
      >
        <div
          className="mb-4 grid size-10 place-items-center rounded-full bg-[#edf7f1] text-lg font-bold text-[#176b61]"
          aria-hidden="true"
        >
          ✓
        </div>
        <h2
          className="text-xl font-semibold text-[#172b2a]"
          id="registration-success-title"
        >
          Registration complete
        </h2>
        <p className="mt-2 text-sm leading-6 text-[#586b68]">
          You have been successfully registered. You can now log in with your
          new account.
        </p>
        <button
          className="mt-6 h-11 w-full rounded-md bg-[#176b61] px-4 text-sm font-bold text-white hover:bg-[#12594f]"
          onClick={() => navigate("/login", { replace: true, state: null })}
          type="button"
        >
          Continue to login
        </button>
      </section>
    </div>
  );
}

export default RegistrationSuccessDialog;
