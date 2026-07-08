"use client";

import { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Download, Menu, X } from "lucide-react";
import Image from "next/image";

const navLinks = [
  { label: "特性", href: "#features" },
  { label: "数据", href: "#stats" },
  { label: "赛事", href: "#events" },
  { label: "下载", href: "#download" },
];

export default function Navbar() {
  const [scrolled, setScrolled] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    const handler = () => setScrolled(window.scrollY > 40);
    window.addEventListener("scroll", handler);
    return () => window.removeEventListener("scroll", handler);
  }, []);

  return (
    <motion.nav
      initial={{ y: -80, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      transition={{ duration: 0.7, ease: "easeOut" }}
      className={`fixed top-0 left-0 right-0 z-50 transition-all duration-500 ${
        scrolled ? "glass border-b border-[rgba(255,85,0,0.15)] py-3" : "py-5"
      }`}
    >
      <div className="max-w-7xl mx-auto px-6 flex items-center justify-between">
        {/* Logo */}
        <a href="#" className="flex items-center gap-3 group">
          <div className="relative w-10 h-10 flex-shrink-0">
            <div className="absolute inset-0 bg-[#FF5500] rounded-full opacity-20 group-hover:opacity-40 transition-opacity blur-lg" />
            <Image
              src="/ycp_emblem.png"
              alt="YCP Logo"
              fill
              className="object-contain relative z-10 drop-shadow-lg"
              style={{ filter: "drop-shadow(0 0 8px rgba(255,85,0,0.6))" }}
            />
          </div>
          <span
            className="font-black text-lg tracking-widest uppercase"
            style={{ fontFamily: "var(--font-outfit)" }}
          >
            YACHIYO
            <span className="text-[#FF5500] ml-1">CUP</span>
          </span>
        </a>

        {/* Desktop Nav */}
        <ul className="hidden md:flex items-center gap-8">
          {navLinks.map((l) => (
            <li key={l.label}>
              <a
                href={l.href}
                className="text-sm font-semibold tracking-widest uppercase text-gray-400 hover:text-[#FF5500] transition-colors duration-200 relative group"
              >
                {l.label}
                <span className="absolute -bottom-1 left-0 w-0 h-px bg-[#FF5500] group-hover:w-full transition-all duration-300" />
              </a>
            </li>
          ))}
        </ul>

        {/* CTA */}
        <a href="#download" className="hidden md:flex btn-orange text-sm py-3 px-6">
          <Download size={15} />
          立即下载
        </a>

        {/* Mobile Toggle */}
        <button
          id="nav-mobile-toggle"
          className="md:hidden text-gray-300 hover:text-[#FF5500] transition-colors"
          onClick={() => setMobileOpen(!mobileOpen)}
          aria-label="Toggle menu"
        >
          {mobileOpen ? <X size={24} /> : <Menu size={24} />}
        </button>
      </div>

      {/* Mobile Drawer */}
      <AnimatePresence>
        {mobileOpen && (
          <motion.div
            key="mobile-menu"
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: "auto" }}
            exit={{ opacity: 0, height: 0 }}
            className="md:hidden glass border-t border-[rgba(255,85,0,0.12)] px-6 py-4 flex flex-col gap-4"
          >
            {navLinks.map((l) => (
              <a
                key={l.label}
                href={l.href}
                className="text-sm font-semibold tracking-widest uppercase text-gray-400 hover:text-[#FF5500] transition-colors"
                onClick={() => setMobileOpen(false)}
              >
                {l.label}
              </a>
            ))}
            <a href="#download" className="btn-orange text-sm py-3 px-6 text-center">
              <Download size={15} />
              立即下载
            </a>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.nav>
  );
}
