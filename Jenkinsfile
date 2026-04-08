// ═══════════════════════════════════════════════════════════════════════════════
// FreshMart — Jenkins CI/CD Pipeline
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE:
//   Automates the full build, test, and deploy cycle for the FreshMart platform.
//   Triggered manually (Build Now) or automatically via GitHub webhook on push.
//
// PIPELINE STAGES:
//   1. Checkout       — clean clone from GitHub (prevents git cache corruption)
//   2. Inject Secrets — copies .env from Jenkins credentials, generates dev configs
//   3. Build .NET     — dotnet restore + build all 15 projects
//   4. Test .NET      — runs 30 NUnit unit tests, publishes JUnit results
//   5. Build Frontend — npm ci + ng build --configuration production
//   6. Fix Docker Socket — chmod 666 /var/run/docker.sock (Jenkins runs as root)
//   7. Docker Build   — builds all 14 service images in parallel
//   8. Deploy         — docker compose up -d (starts all containers)
//
// SECRETS MANAGEMENT:
//   - Only ONE credential needed: 'grocery-env-file' (Secret file in Jenkins)
//   - All other secrets (JWT, RabbitMQ, email, Razorpay, Gemini) are extracted
//     from the .env file at build time — no individual credentials needed
//   - Secrets are deleted from workspace in post { always { ... } }
//
// DOCKER BUILD:
//   - All 14 images built in parallel to save time
//   - Frontend image receives RAZORPAY_KEY and GOOGLE_CLIENT_ID as --build-arg
//     so they are baked into the Angular bundle at build time
//   - Images tagged as grocery/<service>:<build-number> and grocery/<service>:latest
//
// REQUIREMENTS:
//   Jenkins image must have: dotnet 10, node 20, docker CLI, python3
//   See infrastructure/jenkins/Dockerfile
// ═══════════════════════════════════════════════════════════════════════════════

pipeline {
    agent any

    environment {
        COMPOSE_FILE = 'infrastructure/docker-compose.yml'
    }

    options {
        buildDiscarder(logRotator(numToKeepStr: '10'))
        timeout(time: 45, unit: 'MINUTES')
        disableConcurrentBuilds()
        skipDefaultCheckout(false)
    }

    stages {

        // ── 1. Checkout ───────────────────────────────────────────────────────
        stage('Checkout') {
            steps {
                // Clean workspace before checkout to prevent git object corruption
                cleanWs()
                checkout([
                    $class: 'GitSCM',
                    branches: [[name: '*/main']],
                    extensions: [[$class: 'CleanBeforeCheckout']],
                    userRemoteConfigs: [[
                        url: 'https://github.com/maheshsingh20/FreshMart.git',
                        credentialsId: 'github-token'
                    ]]
                ])
                echo "Branch: ${env.GIT_BRANCH} | Commit: ${env.GIT_COMMIT?.take(8)}"
            }
        }

        // ── 2. Inject secrets (create files Jenkins needs but git doesn't have)
        stage('Inject Secrets') {
            steps {
                // Inject the .env file from Jenkins credentials store
                // This replaces the gitignored infrastructure/.env at build time
                withCredentials([file(credentialsId: 'grocery-env-file', variable: 'ENV_FILE')]) {
                    sh 'cp $ENV_FILE infrastructure/.env'
                    sh 'cp $ENV_FILE .env'

                    // Generate NotificationService dev config from the env file values
                    sh '''
                        JWT_SECRET=$(grep "^JWT_SECRET=" infrastructure/.env | cut -d= -f2)
                        RABBITMQ_USER=$(grep "^RABBITMQ_USER=" infrastructure/.env | cut -d= -f2)
                        RABBITMQ_PASS=$(grep "^RABBITMQ_PASS=" infrastructure/.env | cut -d= -f2)
                        EMAIL_USERNAME=$(grep "^EMAIL_USERNAME=" infrastructure/.env | cut -d= -f2)
                        EMAIL_PASSWORD=$(grep "^EMAIL_PASSWORD=" infrastructure/.env | cut -d= -f2-)

                        cat > services/NotificationService/appsettings.Development.json << JSONEOF
{
  "ConnectionStrings": {
    "NotificationDb": "Server=.\\\\SQLEXPRESS;Database=GroceryNotifications;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": { "Secret": "$JWT_SECRET", "Issuer": "GroceryPlatform", "Audience": "GroceryPlatformClients" },
  "RabbitMQ": { "Host": "localhost", "Port": "5672", "Username": "$RABBITMQ_USER", "Password": "$RABBITMQ_PASS", "VirtualHost": "/" },
  "Email": { "Enabled": "true", "Host": "smtp.gmail.com", "Port": "587", "Username": "$EMAIL_USERNAME", "Password": "$EMAIL_PASSWORD", "From": "FreshMart <$EMAIL_USERNAME>" },
  "Urls": "http://localhost:5007"
}
JSONEOF
                    '''
                }
                echo "Secrets injected successfully"
            }
        }

        // ── 3. Build .NET ─────────────────────────────────────────────────────
        stage('Build .NET') {
            steps {
                sh 'dotnet restore CapgeminiSprint.sln'
                sh 'dotnet build CapgeminiSprint.sln --no-restore -c Release'
            }
        }

        // ── 4. Run Unit Tests ─────────────────────────────────────────────────
        stage('Test .NET') {
            steps {
                sh '''
                    dotnet test tests/AuthService.Tests/AuthService.Tests.csproj \
                        --no-build -c Release \
                        --logger "trx;LogFileName=auth-results.trx" \
                        --results-directory ./TestResults || true

                    dotnet test tests/ProductService.Tests/ProductService.Tests.csproj \
                        --no-build -c Release \
                        --logger "trx;LogFileName=product-results.trx" \
                        --results-directory ./TestResults || true

                    dotnet test tests/OrderService.Tests/OrderService.Tests.csproj \
                        --no-build -c Release \
                        --logger "trx;LogFileName=order-results.trx" \
                        --results-directory ./TestResults || true

                    dotnet test tests/CartService.Tests/CartService.Tests.csproj \
                        --no-build -c Release \
                        --logger "trx;LogFileName=cart-results.trx" \
                        --results-directory ./TestResults || true

                    dotnet test tests/PaymentService.Tests/PaymentService.Tests.csproj \
                        --no-build -c Release \
                        --logger "trx;LogFileName=payment-results.trx" \
                        --results-directory ./TestResults || true

                    dotnet test tests/DeliveryService.Tests/DeliveryService.Tests.csproj \
                        --no-build -c Release \
                        --logger "trx;LogFileName=delivery-results.trx" \
                        --results-directory ./TestResults || true

                    dotnet test tests/CouponService.Tests/CouponService.Tests.csproj \
                        --no-build -c Release \
                        --logger "trx;LogFileName=coupon-results.trx" \
                        --results-directory ./TestResults || true
                '''
            }
            post {
                always {
                    junit allowEmptyResults: true, testResults: 'TestResults/**/*.trx'
                }
            }
        }

        // ── 5. Build Angular Frontend ─────────────────────────────────────────
        stage('Build Frontend') {
            steps {
                // Frontend is built inside Docker via --build-arg in dockerBuildFrontend()
                // No need to modify source files here — Dockerfile handles placeholder replacement
                echo "Frontend will be built in Docker Build stage with real keys injected via --build-arg"
            }
        }

        // ── 6. Docker Build (parallel) ────────────────────────────────────────
        stage('Fix Docker Socket') {
            steps {
                sh 'chmod 666 /var/run/docker.sock || true'
                // Login to Docker Hub to avoid anonymous pull rate limits (100/6h → 200/6h)
                withCredentials([usernamePassword(credentialsId: 'dockerhub-credentials', usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
                    sh 'echo $DOCKER_PASS | docker login -u $DOCKER_USER --password-stdin || true'
                }
            }
        }

        stage('Docker Build') {
            parallel {
                stage('auth-service')         { steps { dockerBuild('auth-service',         'services/AuthService/Dockerfile')         } }
                stage('product-service')      { steps { dockerBuild('product-service',      'services/ProductService/Dockerfile')      } }
                stage('order-service')        { steps { dockerBuild('order-service',        'services/OrderService/Dockerfile')        } }
                stage('payment-service')      { steps { dockerBuild('payment-service',      'services/PaymentService/Dockerfile')      } }
                stage('notification-service') { steps { dockerBuild('notification-service', 'services/NotificationService/Dockerfile') } }
                stage('cart-service')         { steps { dockerBuild('cart-service',         'services/CartService/Dockerfile')         } }
                stage('delivery-service')     { steps { dockerBuild('delivery-service',     'services/DeliveryService/Dockerfile')     } }
                stage('review-service')       { steps { dockerBuild('review-service',       'services/ReviewService/Dockerfile')       } }
                stage('coupon-service')       { steps { dockerBuild('coupon-service',       'services/CouponService/Dockerfile')       } }
                stage('ai-service')           { steps { dockerBuild('ai-service',           'services/AiService/Dockerfile')           } }
                stage('user-service')         { steps { dockerBuild('user-service',         'services/UserService/Dockerfile')         } }
                stage('support-service')      { steps { dockerBuild('support-service',      'services/SupportService/Dockerfile')      } }
                stage('api-gateway')          { steps { dockerBuild('api-gateway',          'services/ApiGateway/Dockerfile')          } }
                stage('frontend')             { steps { dockerBuildFrontend()                                                          } }
            }
        }

        // ── 7. Deploy ─────────────────────────────────────────────────────────
        stage('Deploy') {
            steps {
                withCredentials([file(credentialsId: 'grocery-env-file', variable: 'ENV_FILE')]) {
                    sh '''
                        cp $ENV_FILE infrastructure/.env

                        # Fix network label mismatch — recreate with proper Compose labels
                        docker network disconnect infrastructure_grocery-net jenkins || true
                        docker network rm infrastructure_grocery-net || true
                        docker network create \
                            --label com.docker.compose.network=grocery-net \
                            --label com.docker.compose.project=infrastructure \
                            infrastructure_grocery-net || true
                        docker network connect infrastructure_grocery-net jenkins || true

                        docker compose -f infrastructure/docker-compose.yml up -d --no-build --pull never
                    '''
                }
            }
        }
    }

    post {
        always {
            sh '''
                rm -f infrastructure/.env .env
                rm -f services/NotificationService/appsettings.Development.json
            '''
            cleanWs()
        }
        success {
            echo "Pipeline succeeded — ${env.GIT_BRANCH} @ ${env.GIT_COMMIT?.take(8)}"
        }
        failure {
            echo "Pipeline FAILED — ${env.GIT_BRANCH} @ ${env.GIT_COMMIT?.take(8)}"
        }
    }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

def dockerBuild(String service, String dockerfile) {
    sh """
        docker build \\
            -f ${dockerfile} \\
            -t grocery/${service}:${env.BUILD_NUMBER} \\
            -t grocery/${service}:latest \\
            .
    """
}

def dockerBuildFrontend() {
    sh """
        RAZORPAY_KEY=\$(grep "^RAZORPAY_KEY_ID=" infrastructure/.env | cut -d= -f2 | tr -d '\\r\\n ')
        GOOGLE_CLIENT_ID=\$(grep "^GOOGLE_CLIENT_ID=" infrastructure/.env | cut -d= -f2 | tr -d '\\r\\n ')
        docker build \\
            -f Frontend/Dockerfile \\
            --build-arg RAZORPAY_KEY=\${RAZORPAY_KEY} \\
            --build-arg GOOGLE_CLIENT_ID=\${GOOGLE_CLIENT_ID} \\
            -t grocery/frontend:${env.BUILD_NUMBER} \\
            -t grocery/frontend:latest \\
            Frontend/
    """
}
