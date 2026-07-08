"use client";

import { motion } from "framer-motion";
import { Download, Zap, Trophy, Star } from "lucide-react";

export default function CTASection() {
  return (
    <section id="download" className="relative py-40 px-6 overflow-hidden">
      {/* Dramatic background */}
      <div className="absolute inset-0 grid-bg opacity-30 pointer-events-none" />
      <div
        className="absolute inset-0 pointer-events-none"
        style={{
          background:
            "radial-gradient(ellipse at center bottom, rgba(255,85,0,0.18) 0%, transparent 65%)",
        }}
      />

      {/* Horizontal scanline */}
      <div
        className="absolute left-0 right-0 h-px top-0 pointer-events-none"
        style={{
          background: "linear-gradient(90deg, transparent, rgba(255,85,0,0.5), transparent)",
        }}
      />

      <div className="max-w-4xl mx-auto px-6 relative z-10 flex flex-col items-center gap-10 text-center">
        {/* Section label */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.6 }}
          className="section-label"
        >
          <Trophy size={12} />
          立即加入
        </motion.div>

        {/* Headline */}
        <motion.h2
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.8, delay: 0.1 }}
          className="font-black leading-none"
          style={{
            fontFamily: "var(--font-outfit)",
            fontSize: "clamp(2.5rem, 7vw, 5.5rem)",
          }}
        >
          <span className="text-white">加入 YACHIYO CUP，</span>
          <br />
          <span className="gradient-text">主宰你的比赛。</span>
        </motion.h2>

        <motion.p
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true }}
          transition={{ duration: 0.8, delay: 0.3 }}
          className="text-gray-400 text-lg max-w-2xl leading-relaxed"
        >
          超过 100 万名玩家已经选择 YCP Launcher 作为他们的战场指挥中心。现在，轮到你了。
        </motion.p>

        {/* Feature chips */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.7, delay: 0.4 }}
          className="flex flex-wrap gap-3 justify-center"
        >
          {[
            { icon: <Zap size={13} />, label: "一键连接" },
            { icon: <Star size={13} />, label: "免费下载" },
            { icon: <Download size={13} />, label: "自动更新" },
          ].map((c) => (
            <div
              key={c.label}
              className="flex items-center gap-2 px-4 py-2 rounded-full text-xs font-semibold text-gray-300 border border-[rgba(255,85,0,0.2)] bg-[rgba(255,85,0,0.05)]"
            >
              <span className="text-[#FF5500]">{c.icon}</span>
              {c.label}
            </div>
          ))}
        </motion.div>

        {/* Buttons */}
        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          whileInView={{ opacity: 1, scale: 1 }}
          viewport={{ once: true }}
          transition={{ duration: 0.6, delay: 0.5 }}
          className="flex flex-col sm:flex-row gap-4"
        >
          <a
            id="cta-download-btn"
            href="/api/download"
            className="btn-orange text-base px-12 py-5"
          >
            <Download size={20} />
            免费下载 YCP Launcher V2
          </a>
          <a href="#features" className="btn-outline text-sm px-8 py-4">
            了解更多特性
          </a>
        </motion.div>

        {/* Glass stats bar */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.8, delay: 0.6 }}
          className="glass-card p-6 flex flex-col sm:flex-row gap-8 items-center justify-center w-full max-w-2xl mt-4"
        >
          {[
            { n: "1M+", l: "活跃玩家" },
            { n: "V2.1.0", l: "最新版本" },
            { n: "45 MB", l: "安装包大小" },
            { n: "Win 10/11", l: "系统支持" },
          ].map((s) => (
            <div key={s.l} className="flex flex-col items-center gap-1">
              <span className="text-xl font-black text-[#FF5500]">{s.n}</span>
              <span className="text-xs text-gray-500 tracking-wider">{s.l}</span>
            </div>
          ))}
        </motion.div>
      </div>
    </section>
  );
}
