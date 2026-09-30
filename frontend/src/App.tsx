import { API_BASE_URL } from "./lib/api";

function App() {
  return (
    <main className="min-h-screen overflow-hidden">
      <header className="border-b border-[#dce6e3] bg-white/80">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-5">
          <a
            className="flex items-center gap-3 text-sm font-bold tracking-wide text-[#172b2a]"
            href="/"
          >
            <span className="grid size-9 place-items-center rounded-lg bg-[#176b61] text-white">
              M
            </span>
            MICRO / SERVICES
          </a>
          <span className="rounded-full border border-[#cce2d9] bg-[#edf7f1] px-3 py-1 text-xs font-semibold text-[#176b61]">
            FRONTEND ONLINE
          </span>
        </div>
      </header>

      <section className="mx-auto grid max-w-6xl gap-12 px-6 py-16 md:grid-cols-[1fr_0.8fr] md:items-center md:py-24">
        <div className="animate-[rise-in_500ms_ease-out_both]">
          <p className="mb-5 text-xs font-bold uppercase tracking-[0.16em] text-[#bd5936]">
            React + TypeScript + Tailwind
          </p>
          <h1 className="max-w-xl text-4xl font-semibold leading-tight text-[#172b2a] sm:text-5xl">
            Your service workspace starts here.
          </h1>
          <p className="mt-6 max-w-lg text-base leading-7 text-[#586b68]">
            The frontend is connected to the shared API gateway configuration.
            Build service screens here as the product takes shape.
          </p>
        </div>

        <aside className="animate-[rise-in_600ms_100ms_ease-out_both] border-l-2 border-[#e6a23c] bg-white px-6 py-7 shadow-[0_18px_50px_-38px_rgba(23,43,42,0.4)] sm:px-8">
          <p className="text-xs font-bold uppercase tracking-[0.14em] text-[#758682]">
            Gateway target
          </p>
          <p className="mt-3 break-all font-mono text-sm font-medium text-[#176b61]">
            {API_BASE_URL}
          </p>
          <div className="mt-7 border-t border-[#e7eeeb] pt-5">
            <p className="text-xs font-bold uppercase tracking-[0.14em] text-[#758682]">
              Available route groups
            </p>
            <ul className="mt-4 grid grid-cols-2 gap-x-4 gap-y-3 text-sm text-[#334744]">
              <li>
                <span className="mr-2 text-[#bd5936]">/</span>Auth
              </li>
              <li>
                <span className="mr-2 text-[#bd5936]">/</span>Product
              </li>
              <li>
                <span className="mr-2 text-[#bd5936]">/</span>Orders
              </li>
              <li>
                <span className="mr-2 text-[#bd5936]">/</span>Payment
              </li>
              <li>
                <span className="mr-2 text-[#bd5936]">/</span>Logs
              </li>
            </ul>
          </div>
        </aside>
      </section>
    </main>
  );
}

export default App;
