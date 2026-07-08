"use client";

import { useRef } from "react";
import { motion, useInView } from "framer-motion";
import { Shield, BarChart2, Sparkles, RefreshCw, Tv2 } from "lucide-react";
import Image from "next/image";

const features = [
  {
    id: "vac-security",
    icon: <Shield size={28} className="text-[#FF5500]" />,
    tag: "01 // SECURITY",
    title: "一键接入，VAC 级安全防御",
    description:
      "告别繁琐的控制台指令。深度接入 Steam 底层协议 (steam://)，完美支持 VAC 反作弊系统，一键直连 YACHIYO CUP 专属社区服。你只需要点击，系统完成剩下的一切。",
    image: "/feature_launcher.png",
    imageAlt: "VAC security launcher interface",
    highlights: ["Steam 协议深度集成", "VAC 反作弊白名单", "一键社区服直连"],
    centerOverlay: false,
  },
  {
    id: "dashboard",
    icon: <BarChart2 size={28} className="text-[#FF5500]" />,
    tag: "02 // ANALYTICS",
    title: "数据可视化仪表盘",
    description:
      "实时同步玩家的 Rating、K/D Ratio、爆头率等高阶数据。内置每日电竞任务与动态进度条，你的每一场战斗都被精准记录，让进化清晰可见。",
    image: "/feature_dashboard.png",
    imageAlt: "Personal esports data dashboard",
    highlights: ["实时 Rating & K/D", "爆头率分析", "每日任务系统"],
    centerOverlay: true, // ← 使用 ycp_emblem.png 叠加在图片正中间
  },
  {
    id: "animations",
    icon: <Sparkles size={28} className="text-[#FF5500]" />,
    tag: "03 // EXPERIENCE",
    title: "千万级 UI 动效，电影级开场",
    description:
      "彻底摒弃传统软件的廉价感。每次启动伴随 2 秒的深渊浮现开场动画；软件内处处皆是流光溢彩与液态悬浮微动效，享受极致流畅的视觉盛宴。",
    image: "/feature_animation.png",
    imageAlt: "Cinematic launcher animation",
    highlights: ["2s 深渊开场动画", "液态悬浮微动效", "全局流体过渡"],
    centerOverlay: false,
  },
  {
    id: "auto-update",
    icon: <RefreshCw size={28} className="text-[#FF5500]" />,
    tag: "04 // UPDATES",
    title: "无感静默更新 Smart Auto-Update",
    description:
      "内置专业的商业级更新核心。后台静默检测，专属进度条覆盖安装，彻底告别频繁访问官网下载更新包的烦恼。版本永远是最新，体验永远是最优。",
    image: "/feature_update.png",
    imageAlt: "Smart auto-update system",
    highlights: ["后台静默下载", "差量更新技术", "一键覆盖安装"],
    centerOverlay: false,
  },
  {
    id: "esports-news",
    icon: <Tv2 size={28} className="text-[#FF5500]" />,
    tag: "05 // ESPORTS",
    title: "赛事前瞻与 HLTV 级新闻流",
    description:
      "大屏 Banner 轮播赛事资讯，内置 HLTV 级别的实时比分追踪（如 NAVI vs FaZe），不错过任何一场顶尖对决。赛前分析、数据洞察、赛后复盘，一站搞定。",
    image: "/feature_news.png",
    imageAlt: "Live esports news feed",
    highlights: ["实时比分追踪", "赛事 Banner 轮播", "HLTV 级数据源"],
    centerOverlay: false,
  },
];

function FeatureCard({
  feature,
  index,
}: {
  feature: (typeof features)[0];
  index: number;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-60px" });

  return (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, y: 70 }}
      animate={inView ? { opacity: 1, y: 0 } : {}}
      transition={{ duration: 0.85, ease: [0.23, 1, 0.32, 1] }}
      className="flex flex-col items-center gap-8 text-center w-full"
    >
      {/* ── Tag + Icon ─────────────────────────────────────────── */}
      <div className="flex flex-col items-center gap-4">
        <div className="section-label text-[10px]">{feature.tag}</div>
        <div className="icon-box">{feature.icon}</div>
      </div>

      {/* ── Title ──────────────────────────────────────────────── */}
      <h3
        className="text-3xl sm:text-4xl font-black leading-tight text-white max-w-xl"
        style={{ fontFamily: "var(--font-outfit)" }}
      >
        {feature.title}
      </h3>

      {/* ── Description ────────────────────────────────────────── */}
      <p className="text-gray-400 text-base leading-relaxed max-w-lg">
        {feature.description}
      </p>

      {/* ── Bullet highlights ──────────────────────────────────── */}
      <div className="flex flex-wrap gap-3 justify-center">
        {feature.highlights.map((h) => (
          <span
            key={h}
            className="flex items-center gap-2 px-4 py-2 rounded-full text-xs font-semibold text-gray-300
                       border border-[rgba(255,85,0,0.2)] bg-[rgba(255,85,0,0.06)]"
          >
            <span className="w-1.5 h-1.5 rounded-full bg-[#FF5500] shadow-[0_0_6px_#FF5500]" />
            {h}
          </span>
        ))}
      </div>

      {/* ── Feature Image ──────────────────────────────────────── */}
      <motion.div
        initial={{ opacity: 0, scale: 0.96 }}
        animate={inView ? { opacity: 1, scale: 1 } : {}}
        transition={{ duration: 0.9, delay: 0.25, ease: [0.23, 1, 0.32, 1] }}
        className="w-full relative"
      >
        {/* Holographic border wrapper */}
        <div className="holo-border w-full">
          <div className="glass-card overflow-hidden relative w-full">
            {/* Scanline overlay */}
            <div
              className="absolute inset-0 pointer-events-none z-10"
              style={{
                background:
                  "repeating-linear-gradient(0deg,transparent,transparent 3px,rgba(255,85,0,0.04) 3px,rgba(255,85,0,0.04) 4px)",
              }}
            />

            {/* Main image */}
            <Image
              src={feature.image}
              alt={feature.imageAlt}
              width={900}
              height={500}
              className="w-full h-auto object-cover opacity-90 block"
              quality={90}
            />

            {/* ── YCP Emblem overlay for feature #2 ────────────── */}
            {feature.centerOverlay && (
              <div className="absolute inset-0 flex items-center justify-center z-20 pointer-events-none">
                {/* Glow halo */}
                <div
                  className="absolute w-52 h-52 rounded-full"
                  style={{
                    background:
                      "radial-gradient(circle, rgba(255,85,0,0.35) 0%, transparent 70%)",
                    filter: "blur(24px)",
                  }}
                />
                {/* Emblem */}
                <div className="relative w-40 h-40 drop-shadow-2xl">
                  <Image
                    src="/ycp_emblem.png"
                    alt="YACHIYO CUP Emblem"
                    fill
                    className="object-contain"
                    style={{
                      filter:
                        "drop-shadow(0 0 24px rgba(255,85,0,0.9)) drop-shadow(0 0 60px rgba(255,85,0,0.4))",
                    }}
                  />
                </div>
              </div>
            )}

            {/* Corner accents */}
            <div className="absolute top-3 left-3 w-5 h-5 border-t-2 border-l-2 border-[#FF5500] opacity-60 z-20" />
            <div className="absolute top-3 right-3 w-5 h-5 border-t-2 border-r-2 border-[#FF5500] opacity-60 z-20" />
            <div className="absolute bottom-3 left-3 w-5 h-5 border-b-2 border-l-2 border-[#FF5500] opacity-60 z-20" />
            <div className="absolute bottom-3 right-3 w-5 h-5 border-b-2 border-r-2 border-[#FF5500] opacity-60 z-20" />
          </div>
        </div>

        {/* Watermark number */}
        <div
          className="absolute -right-2 -bottom-6 text-[7rem] font-black leading-none select-none pointer-events-none"
          style={{
            fontFamily: "var(--font-outfit)",
            color: "rgba(255,85,0,0.05)",
          }}
        >
          0{index + 1}
        </div>
      </motion.div>
    </motion.div>
  );
}

export default function FeaturesSection() {
  const titleRef = useRef<HTMLDivElement>(null);
  const titleInView = useInView(titleRef, { once: true });

  return (
    <section id="features" className="relative py-32">
      <div className="absolute inset-0 grid-bg opacity-50 pointer-events-none" />

      {/* ── All content strictly centered ─────────────────────────── */}
      <div className="flex flex-col items-center gap-28 px-6" style={{ width: "100%" }}>

        {/* Section header */}
        <motion.div
          ref={titleRef}
          initial={{ opacity: 0, y: 40 }}
          animate={titleInView ? { opacity: 1, y: 0 } : {}}
          transition={{ duration: 0.7 }}
          className="flex flex-col items-center gap-4 text-center"
        >
          <div className="section-label">核心特性</div>
          <h2
            className="text-4xl lg:text-6xl font-black text-white leading-none"
            style={{ fontFamily: "var(--font-outfit)" }}
          >
            BUILT FOR
            <span className="gradient-text"> CHAMPIONS</span>
          </h2>
          <p className="text-gray-400 max-w-xl text-lg">
            五大核心模块，专为职业级体验而生。每一个细节都是对平庸的宣战。
          </p>
        </motion.div>

        {/* Feature cards — each capped at max-w-3xl and centered */}
        {features.map((f, i) => (
          <div key={f.id} className="w-full max-w-3xl mx-auto flex flex-col items-center gap-10">
            <FeatureCard feature={f} index={i} />
            {i < features.length - 1 && (
              <div className="section-divider mt-10 w-full" />
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
