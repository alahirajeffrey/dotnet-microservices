type SubmitButtonProps = {
  busy: boolean;
  children: string;
};

function SubmitButton({ busy, children }: SubmitButtonProps) {
  return (
    <button
      className="mt-2 inline-flex h-12 w-full items-center justify-center rounded-md bg-[#176b61] px-4 text-sm font-bold text-white transition hover:bg-[#12594f] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#176b61] disabled:cursor-wait disabled:opacity-60"
      type="submit"
      disabled={busy}
    >
      {busy ? "Please wait…" : children}
    </button>
  );
}

export default SubmitButton;
