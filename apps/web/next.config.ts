import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  transpilePackages: ['@orchest-flow-ai/web-public'],
  // Required for the Docker standalone runner stage
  output: process.env.DOCKER_BUILD ? 'standalone' : undefined,
};

export default nextConfig;
