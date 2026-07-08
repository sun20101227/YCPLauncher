"use client";

import dynamic from "next/dynamic";
import Navbar from "@/components/Navbar";
import HeroSection from "@/components/HeroSection";
import MarqueeBanner from "@/components/MarqueeBanner";
import FeaturesSection from "@/components/FeaturesSection";
import StatsSection from "@/components/StatsSection";
import CTASection from "@/components/CTASection";
import Footer from "@/components/Footer";

// Particle field runs client-side only (canvas)
const ParticleField = dynamic(() => import("@/components/ParticleField"), {
  ssr: false,
});

export default function Home() {
  return (
    <main className="relative min-h-screen overflow-x-hidden" style={{ background: "#0A0A0A" }}>
      {/* Global ambient particles */}
      <ParticleField />

      {/* Navigation */}
      <Navbar />

      {/* ── Hero ─────────────────────────────────────────────────────── */}
      <HeroSection />

      {/* ── Marquee strip ────────────────────────────────────────────── */}
      <MarqueeBanner />

      {/* ── Features ─────────────────────────────────────────────────── */}
      <FeaturesSection />

      {/* ── Stats + Live Matches ─────────────────────────────────────── */}
      <StatsSection />

      {/* ── CTA Download ─────────────────────────────────────────────── */}
      <CTASection />

      {/* ── Footer ───────────────────────────────────────────────────── */}
      <Footer />
    </main>
  );
}
