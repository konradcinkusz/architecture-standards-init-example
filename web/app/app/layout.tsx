import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "architecture-standards-init-example",
  description: "The estate's default containerized application, scaffolded from the generic template.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
