import { NextResponse } from "next/server";

// ── Replace this URL with the actual release download link ─────────
const LATEST_RELEASE_URL =
  process.env.DOWNLOAD_URL ||
  "https://github.com/yachiyo-cup/ycp-launcher/releases/latest/download/YCPLauncher-Setup.exe";

export async function GET() {
  return NextResponse.json(
    {
      version: "2.1.0",
      url: LATEST_RELEASE_URL,
      fileName: "YCPLauncher-Setup-v2.1.0.exe",
      size: "45 MB",
      releaseDate: "2026-07-01",
      changelog: [
        "全新千万级 UI 动效系统",
        "深度 VAC 协议集成优化",
        "实时数据仪表盘 v2",
        "静默更新引擎升级",
      ],
    },
    { status: 200 }
  );
}
