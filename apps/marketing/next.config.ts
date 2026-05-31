import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "export",
  trailingSlash: true,
  transpilePackages: ['@orchest-flow-ai/web-public'],
};

export default nextConfig;
