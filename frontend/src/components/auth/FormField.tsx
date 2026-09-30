type FormFieldProps = {
  id: string;
  label: string;
  autoComplete: string;
  value: string;
  type?: string;
  onChange: (value: string) => void;
};

function FormField({
  id,
  label,
  type = "text",
  autoComplete,
  value,
  onChange,
}: FormFieldProps) {
  return (
    <label className="block" htmlFor={id}>
      <span className="mb-2 block text-sm font-semibold text-[#334744]">
        {label}
      </span>
      <input
        className="h-12 w-full rounded-md border border-[#cbd8d4] bg-[#fbfdfc] px-3 text-base text-[#172b2a] outline-none transition focus:border-[#176b61] focus:ring-2 focus:ring-[#176b61]/15"
        id={id}
        name={id}
        type={type}
        autoComplete={autoComplete}
        required
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
}

export default FormField;
