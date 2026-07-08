import type { Metadata } from "next";
import { Inter, Outfit } from "next/font/google";
import "./globals.css";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

const outfit = Outfit({
  subsets: ["latin"],
  variable: "--font-outfit",
  display: "swap",
});

export const metadata: Metadata = {
  title: "YACHIYO CUP — Next-Gen Esports Client | YCP Launcher V2",
  description:
    "Experience the most advanced esports launcher. YCP Launcher V2 brings 10-million-dollar aesthetics, VAC-level security, real-time data dashboards, and silent auto-updates. Dominate your match.",
  keywords: ["YACHIYO CUP", "esports launcher", "CS2", "VAC", "gaming client", "YCP"],
  openGraph: {
    title: "YACHIYO CUP — Next-Gen Esports Client",
    description: "Download YCP Launcher V2. Elevate your game.",
    type: "website",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="zh-CN" className={`${inter.variable} ${outfit.variable}`}>
      <body className={`scanlines ${inter.className} antialiased`}>
        {children}
      </body>
    </html>
  );
}
