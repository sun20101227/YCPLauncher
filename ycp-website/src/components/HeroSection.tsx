"use client";

import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import { Download, ChevronRight, Shield, Cpu } from "lucide-react";
import Image from "next/image";

const VERSION = "V2.1.0";
const DL_URL = "/api/download"; // dynamic endpoint

function TypewriterText({ text, delay = 0 }: { text: string; delay?: number }) {
  const [displayed, setDisplayed] = useState("");
  const [started, setStarted] = useState(false);

  useEffect(() => {
    const timeout = setTimeout(() => setStarted(true), delay);
    return () => clearTimeout(timeout);
  }, [delay]);

  useEffect(() => {
    if (!started) return;
    let i = 0;
    const interval = setInterval(() => {
      setDisplayed(text.slice(0, i + 1));
      i++;
      if (i >= text.length) clearInterval(interval);
    }, 50);
    return () => clearInterval(interval);
  }, [started, text]);

  return (
    <span>
      {displayed}
      {displayed.length < text.length && started && (
        <span className="inline-block w-0.5 h-[1em] bg-[#FF5500] ml-1 animate-pulse align-middle" />
      )}
    </span>
  );
}

export default function HeroSection() {
  const [downloadUrl, setDownloadUrl] = useState(DL_URL);

  // Fetch dynamic download link
  useEffect(() => {
    fetch("/api/download")
      .then((r) => r.json())
      .then((d) => d.url && setDownloadUrl(d.url))
      .catch(() => {}); // fallback silently
  }, []);

  return (
    <section
      id="hero"
      className="relative min-h-screen flex flex-col items-center justify-center overflow-hidden grid-bg"
    >
      {/* Hero Background Image */}
      <div className="absolute inset-0 z-0">
        <Image
          src="/hero_bg.png"
          alt="YACHIYO CUP hero background"
          fill
          priority
          className="object-cover opacity-25"
          quality={90}
        />
        {/* Vignette overlay */}
        <div className="absolute inset-0 bg-gradient-to-b from-[#0A0A0A]/60 via-transparent to-[#0A0A0A]" />
        <div className="absolute inset-0 bg-gradient-to-r from-[#0A0A0A]/80 via-transparent to-[#0A0A0A]/80" />
      </div>

      {/* Orange bottom glow */}
      <div
        className="absolute bottom-0 left-1/2 -translate-x-1/2 w-[800px] h-[300px] pointer-events-none z-0"
        style={{
          background: "radial-gradient(ellipse, rgba(255,85,0,0.15) 0%, transparent 70%)",
        }}
      />

      {/* Content */}
      <div className="relative z-10 max-w-4xl mx-auto px-6 flex flex-col items-center text-center gap-8">
        {/* Badge */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6 }}
          className="section-label"
        >
          <span className="pulse-dot" />
          YCP LAUNCHER {VERSION} — 官方发布
        </motion.div>

        {/* Logo */}
        <motion.div
          initial={{ scale: 0.7, opacity: 0 }}
          animate={{ scale: 1, opacity: 1 }}
          transition={{ duration: 0.8, ease: [0.23, 1, 0.32, 1] }}
          className="relative flex items-center justify-center"
        >
          {/* Multi-layer glow */}
          <motion.div
            animate={{ scale: [1, 1.15, 1], opacity: [0.5, 0.85, 0.5] }}
            transition={{ repeat: Infinity, duration: 3, ease: "easeInOut" }}
            className="absolute w-64 h-64 rounded-full pointer-events-none"
            style={{ background: "radial-gradient(circle, rgba(255,85,0,0.45) 0%, transparent 70%)" }}
          />
          <div
            className="absolute w-44 h-44 rounded-full pointer-events-none"
            style={{ background: "radial-gradient(circle, rgba(255,85,0,0.25) 0%, transparent 60%)", filter: "blur(16px)" }}
          />
          <Image
            src="/ycp_emblem.png"
            alt="YACHIYO CUP Logo"
            width={220}
            height={220}
            className="relative z-10"
            style={{ filter: "drop-shadow(0 0 30px rgba(255,85,0,0.9)) drop-shadow(0 0 80px rgba(255,85,0,0.4))" }}
            priority
          />
        </motion.div>

        {/* Main Title */}
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.2 }}
        >
          <h1
            className="font-black leading-none tracking-tight"
            style={{ fontFamily: "var(--font-outfit)", fontSize: "clamp(2.5rem, 8vw, 6rem)" }}
          >
            <span className="text-white">NEXT-GEN </span>
            <br />
            <span className="gradient-text">ESPORTS CLIENT</span>
          </h1>
        </motion.div>

        {/* Subtitle */}
        <motion.p
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 1, delay: 0.5 }}
          className="text-gray-400 text-lg max-w-2xl leading-relaxed"
        >
          <TypewriterText
            text="不只是启动器——这是你征战 YACHIYO CUP 的指挥中心。深度接入 Steam 底层协议，千万级 UI 动效，主宰你的每一场对决。"
            delay={800}
          />
        </motion.p>

        {/* Trust badges */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.9 }}
          className="flex flex-wrap gap-4 justify-center"
        >
          {[
            { icon: <Shield size={14} />, label: "VAC 级安全" },
            { icon: <Cpu size={14} />, label: "Steam 底层协议" },
            { icon: <Download size={14} />, label: "静默自动更新" },
          ].map((b) => (
            <div
              key={b.label}
              className="flex items-center gap-2 px-4 py-2 rounded-full text-xs font-semibold text-gray-400 border border-[rgba(255,85,0,0.15)] bg-[rgba(255,85,0,0.05)]"
            >
              <span className="text-[#FF5500]">{b.icon}</span>
              {b.label}
            </div>
          ))}
        </motion.div>

        {/* CTA Buttons */}
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7, delay: 1.1 }}
          className="flex flex-col sm:flex-row gap-4 mt-2"
        >
          <a
            id="hero-download-btn"
            href={downloadUrl}
            className="btn-orange text-base px-10 py-5"
          >
            <Download size={18} />
            立即下载 YCP Launcher {VERSION}
          </a>
          <a href="#features" className="btn-outline text-sm px-8 py-4">
            探索特性
            <ChevronRight size={16} />
          </a>
        </motion.div>

        {/* Version note */}
        <motion.p
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 1.4 }}
          className="text-xs text-gray-600 tracking-wider"
        >
          Windows 10/11 · x64 · 45 MB · 免费下载
        </motion.p>
      </div>

      {/* Scroll indicator */}
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 2 }}
        className="absolute bottom-10 left-1/2 -translate-x-1/2 flex flex-col items-center gap-2"
      >
        <span className="text-xs text-gray-600 tracking-widest uppercase">Scroll</span>
        <motion.div
          animate={{ y: [0, 8, 0] }}
          transition={{ repeat: Infinity, duration: 1.5 }}
          className="w-px h-8 bg-gradient-to-b from-[#FF5500] to-transparent"
        />
      </motion.div>
    </section>
  );
}
