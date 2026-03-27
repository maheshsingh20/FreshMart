# Stage 1: Build Angular app
FROM node:22-alpine AS build

# Build args for injecting secrets at build time
ARG RAZORPAY_KEY=RAZORPAY_KEY_PLACEHOLDER
ARG GOOGLE_CLIENT_ID=GOOGLE_CLIENT_ID_PLACEHOLDER

WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .

# Replace placeholders with actual values before building
RUN sed -i "s|RAZORPAY_KEY_PLACEHOLDER|${RAZORPAY_KEY}|g" src/environments/environment.ts \
    && sed -i "s|RAZORPAY_KEY_PLACEHOLDER|${RAZORPAY_KEY}|g" src/environments/environment.prod.ts \
    && sed -i "s|GOOGLE_CLIENT_ID_PLACEHOLDER|${GOOGLE_CLIENT_ID}|g" src/environments/environment.ts \
    && sed -i "s|GOOGLE_CLIENT_ID_PLACEHOLDER|${GOOGLE_CLIENT_ID}|g" src/environments/environment.prod.ts

RUN npm run build -- --configuration production

# Stage 2: Serve with nginx
FROM nginx:alpine
COPY --from=build /app/dist/Frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
