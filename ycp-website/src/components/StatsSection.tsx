"use client";

import { useRef } from "react";
import { motion, useInView } from "framer-motion";
import Image from "next/image";

const stats = [
  { value: "1M+", label: "活跃玩家", sublabel: "Active Players" },
  { value: "99.9%", label: "服务器稳定性", sublabel: "Server Uptime" },
  { value: "<5ms", label: "指令响应延迟", sublabel: "Command Latency" },
  { value: "0", label: "已知 VAC 误封", sublabel: "VAC False Bans" },
];

const matchData = [
  {
    id: "m1",
    team1: { name: "NAVI", flag: "🇺🇦", score: 16 },
    team2: { name: "FaZe", flag: "🇪🇺", score: 11 },
    map: "Mirage",
    status: "LIVE",
    elapsed: "42:18",
  },
  {
    id: "m2",
    team1: { name: "G2", flag: "🇫🇷", score: 9 },
    team2: { name: "Vitality", flag: "🇫🇷", score: 14 },
    map: "Dust2",
    status: "LIVE",
    elapsed: "31:55",
  },
  {
    id: "m3",
    team1: { name: "Astralis", flag: "🇩🇰", score: 16 },
    team2: { name: "Liquid", flag: "🇺🇸", score: 7 },
    map: "Inferno",
    status: "ENDED",
    elapsed: "52:10",
  },
];

function StatCard({
  stat,
  index,
}: {
  stat: (typeof stats)[0];
  index: number;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true });

  return (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, y: 40 }}
      animate={inView ? { opacity: 1, y: 0 } : {}}
      transition={{ duration: 0.6, delay: index * 0.1 }}
      className="glass-card p-8 flex flex-col gap-2 text-center group"
    >
      <div className="stat-number group-hover:text-glow transition-all duration-300">
        {stat.value}
      </div>
      <div className="text-white font-semibold text-sm">{stat.label}</div>
      <div className="text-gray-500 text-xs tracking-widest uppercase">
        {stat.sublabel}
      </div>
    </motion.div>
  );
}

function TeamLogo({ name }: { name: string }) {
  return (
    <div className="relative w-10 h-10 rounded-lg overflow-hidden border border-[rgba(255,85,0,0.3)] bg-[rgba(255,85,0,0.08)] flex-shrink-0">
      <Image
        src="/ycp_emblem.png"
        alt={`${name} logo`}
        fill
        className="object-cover opacity-70"
      />
      <div className="absolute inset-0 flex items-center justify-center">
        <span className="font-black text-[#FF5500] text-sm drop-shadow-lg"
          style={{ textShadow: "0 0 8px rgba(255,85,0,0.8)" }}>
          {name[0]}
        </span>
      </div>
    </div>
  );
}

function MatchCard({ match, index }: { match: (typeof matchData)[0]; index: number }) {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true });
  const isLive = match.status === "LIVE";

  return (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, x: -30 }}
      animate={inView ? { opacity: 1, x: 0 } : {}}
      transition={{ duration: 0.6, delay: index * 0.12 }}
      className="glass-card p-5 flex items-center justify-between gap-4"
    >
      {/* Team 1 */}
      <div className="flex items-center gap-3 flex-1">
        <TeamLogo name={match.team1.name} />
        <div>
          <div className="font-bold text-sm text-white">{match.team1.name}</div>
          <div className="text-xs text-gray-500">{match.team1.flag}</div>
        </div>
      </div>

      {/* Score + Status */}
      <div className="flex flex-col items-center gap-1">
        <div className="flex items-center gap-3">
          <span className="text-2xl font-black text-white">{match.team1.score}</span>
          <span className="text-gray-600 font-bold">:</span>
          <span className="text-2xl font-black text-white">{match.team2.score}</span>
        </div>

        {/* Centre emblem */}
        <div className="relative w-6 h-6 my-0.5 opacity-60">
          <Image src="/ycp_emblem.png" alt="YCP" fill className="object-contain" />
        </div>

        <div className="flex flex-col items-center gap-0.5">
          {isLive ? (
            <div className="live-badge">
              <span className="pulse-dot" style={{ background: "#ef4444", boxShadow: "0 0 6px #ef4444" }} />
              LIVE
            </div>
          ) : (
            <div className="text-[10px] text-gray-500 tracking-wider uppercase font-semibold">
              Ended
            </div>
          )}
          <div className="text-[10px] text-gray-600 tracking-wider">{match.map} · {match.elapsed}</div>
        </div>
      </div>

      {/* Team 2 */}
      <div className="flex items-center gap-3 flex-1 justify-end text-right">
        <div>
          <div className="font-bold text-sm text-white">{match.team2.name}</div>
          <div className="text-xs text-gray-500">{match.team2.flag}</div>
        </div>
        <TeamLogo name={match.team2.name} />
      </div>
    </motion.div>
  );
}

export default function StatsSection() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true });

  return (
    <section id="stats" className="relative py-32 overflow-hidden">
      {/* Glow backdrop */}
      <div
        className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[400px] pointer-events-none"
        style={{
          background: "radial-gradient(ellipse, rgba(255,85,0,0.08) 0%, transparent 70%)",
        }}
      />

      <div className="max-w-4xl mx-auto px-6 flex flex-col gap-20">
        {/* Stats Grid */}
        <div>
          <motion.div
            ref={ref}
            initial={{ opacity: 0, y: 30 }}
            animate={inView ? { opacity: 1, y: 0 } : {}}
            className="text-center flex flex-col items-center gap-4 mb-12"
          >
            <div className="section-label">平台数据</div>
            <h2
              className="text-4xl lg:text-5xl font-black text-white"
              style={{ fontFamily: "var(--font-outfit)" }}
            >
              NUMBERS DON&apos;T
              <span className="gradient-text"> LIE</span>
            </h2>
          </motion.div>

          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {stats.map((s, i) => (
              <StatCard key={s.label} stat={s} index={i} />
            ))}
          </div>
        </div>

        <div className="section-divider" />

        {/* Live Matches */}
        <div id="events">
          <motion.div
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.7 }}
            className="text-center flex flex-col items-center gap-4 mb-12"
          >
            <div className="section-label">
              {/* YCP emblem as section icon */}
              <span className="relative w-4 h-4 inline-block align-middle mr-1">
                <Image src="/ycp_emblem.png" alt="YCP" fill className="object-contain" />
              </span>
              赛事中心
            </div>
            <h2
              className="text-4xl lg:text-5xl font-black text-white"
              style={{ fontFamily: "var(--font-outfit)" }}
            >
              LIVE
              <span className="gradient-text"> MATCHES</span>
            </h2>
            <p className="text-gray-400 max-w-lg">
              实时追踪全球顶级赛事比分，不错过任何精彩瞬间
            </p>
          </motion.div>

          <div className="flex flex-col gap-4 max-w-2xl mx-auto">
            {matchData.map((m, i) => (
              <MatchCard key={m.id} match={m} index={i} />
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
