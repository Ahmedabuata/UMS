FROM node:20-alpine
RUN npm install -g opencode-ai@latest
WORKDIR /app
EXPOSE 4096
CMD ["opencode", "serve", "--hostname", "0.0.0.0", "--port", "4096"]
