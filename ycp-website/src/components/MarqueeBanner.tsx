"use client";

const brands = [
  "YACHIYO CUP", "YCP LAUNCHER V2", "VAC SECURED",
  "STEAM PROTOCOL", "NEXT-GEN ESPORTS", "1,000,000 PLAYERS",
  "HLTV LEVEL", "AUTO UPDATE", "YACHIYO CUP",
];

export default function MarqueeBanner() {
  return (
    <div
      className="relative py-4 overflow-hidden"
      style={{
        borderTop: "1px solid rgba(255,85,0,0.15)",
        borderBottom: "1px solid rgba(255,85,0,0.15)",
        background: "rgba(255,85,0,0.03)",
      }}
    >
      <div className="marquee-track">
        {[...brands, ...brands].map((b, i) => (
          <span
            key={i}
            className="flex items-center gap-6 whitespace-nowrap text-xs font-black tracking-[0.2em] uppercase"
          >
            <span className="text-[#FF5500] opacity-60">◆</span>
            <span className="text-gray-500">{b}</span>
          </span>
        ))}
      </div>
    </div>
  );
}
