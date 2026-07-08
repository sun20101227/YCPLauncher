import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  images: {
    remotePatterns: [],
    // local public images are served automatically
  },
  // Allow framer-motion server components
  transpilePackages: [],
};

export default nextConfig;
