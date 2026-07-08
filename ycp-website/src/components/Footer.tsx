"use client";

import { GitBranch, X, MessageCircle } from "lucide-react";
import Image from "next/image";

const footerLinks = {
  "产品": ["下载中心", "版本说明", "系统要求", "更新日志"],
  "支持": ["常见问题", "联系客服", "Bug 反馈", "用户手册"],
  "社区": ["Discord", "官方论坛", "Twitter", "Bilibili"],
  "法律": ["隐私政策", "服务条款", "Cookie 政策", "DMCA"],
};

export default function Footer() {
  return (
    <footer className="relative border-t border-[rgba(255,85,0,0.1)] bg-[#0A0A0A] px-6 py-16">
      <div className="max-w-4xl mx-auto flex flex-col gap-12">
        {/* Top row */}
        <div className="grid grid-cols-2 md:grid-cols-5 gap-10">
          {/* Brand */}
          <div className="col-span-2 md:col-span-1 flex flex-col gap-4">
            <div className="flex items-center gap-2">
              <div className="relative w-8 h-8 flex-shrink-0">
                <Image
                  src="/ycp_emblem.png"
                  alt="YCP"
                  fill
                  className="object-contain"
                  style={{ filter: "drop-shadow(0 0 6px rgba(255,85,0,0.5))" }}
                />
              </div>
              <span
                className="font-black text-base tracking-widest uppercase"
                style={{ fontFamily: "var(--font-outfit)" }}
              >
                YACHIYO
                <span className="text-[#FF5500] ml-1">CUP</span>
              </span>
            </div>
            <p className="text-gray-500 text-sm leading-relaxed">
              次世代电竞平台。为冠军而生。
            </p>
            {/* Social links */}
            <div className="flex gap-3 mt-2">
              {[
                { icon: <X size={16} />, label: "Twitter/X", href: "#" },
                { icon: <MessageCircle size={16} />, label: "Discord", href: "#" },
                { icon: <GitBranch size={16} />, label: "Github", href: "#" },
              ].map((s) => (
                <a
                  key={s.label}
                  href={s.href}
                  aria-label={s.label}
                  className="w-9 h-9 rounded-lg border border-[rgba(255,85,0,0.2)] flex items-center justify-center text-gray-500 hover:text-[#FF5500] hover:border-[rgba(255,85,0,0.5)] transition-all duration-200"
                >
                  {s.icon}
                </a>
              ))}
            </div>
          </div>

          {/* Link columns */}
          {Object.entries(footerLinks).map(([title, links]) => (
            <div key={title} className="flex flex-col gap-3">
              <h4 className="text-xs font-bold tracking-widest uppercase text-gray-500">
                {title}
              </h4>
              <ul className="flex flex-col gap-2">
                {links.map((link) => (
                  <li key={link}>
                    <a
                      href="#"
                      className="text-sm text-gray-400 hover:text-[#FF5500] transition-colors duration-200"
                    >
                      {link}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        {/* Divider */}
        <div className="section-divider" />

        {/* Bottom row */}
        <div className="flex flex-col sm:flex-row justify-between items-center gap-4 text-xs text-gray-600">
          <p>
            © {new Date().getFullYear()} YACHIYO CUP. All rights reserved. Powered by YCP Platform.
          </p>
          <div className="flex gap-6">
            <a href="#" className="hover:text-[#FF5500] transition-colors">隐私政策</a>
            <a href="#" className="hover:text-[#FF5500] transition-colors">服务条款</a>
            <a
              href="mailto:support@yachiyocup.gg"
              className="hover:text-[#FF5500] transition-colors"
            >
              联系客服
            </a>
          </div>
        </div>
      </div>

      {/* Giant background watermark */}
      <div
        className="absolute bottom-0 right-8 text-[12rem] font-black leading-none select-none pointer-events-none"
        style={{
          fontFamily: "var(--font-outfit)",
          color: "rgba(255,85,0,0.03)",
        }}
      >
        YCP
      </div>
    </footer>
  );
}
