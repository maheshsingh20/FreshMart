pipeline {
    agent any

    environment {
        COMPOSE_FILE = 'infrastructure/docker-compose.yml'
    }

    options {
        buildDiscarder(logRotator(numToKeepStr: '10'))
        timeout(time: 45, unit: 'MINUTES')
        disableConcurrentBuilds()
    }

    stages {

        // ── 1. Checkout ───────────────────────────────────────────────────────
        stage('Checkout') {
            steps {
                checkout scm
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
                        --logger "trx;LogFileName=results.trx" \
                        --results-directory ./TestResults \
                        || true
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
                // Inject frontend keys into environment files before build
                withCredentials([
                    string(credentialsId: 'razorpay-key-id',   variable: 'RAZORPAY_KEY'),
                    string(credentialsId: 'google-client-id',  variable: 'GOOGLE_CLIENT_ID')
                ]) {
                    sh '''
                        sed -i "s|RAZORPAY_KEY_PLACEHOLDER|${RAZORPAY_KEY}|g" Frontend/src/environments/environment.ts
                        sed -i "s|RAZORPAY_KEY_PLACEHOLDER|${RAZORPAY_KEY}|g" Frontend/src/environments/environment.prod.ts
                        sed -i "s|GOOGLE_CLIENT_ID_PLACEHOLDER|${GOOGLE_CLIENT_ID}|g" Frontend/src/environments/environment.ts
                        sed -i "s|GOOGLE_CLIENT_ID_PLACEHOLDER|${GOOGLE_CLIENT_ID}|g" Frontend/src/environments/environment.prod.ts
                    '''
                }
                dir('Frontend') {
                    sh 'npm ci --prefer-offline'
                    sh 'npm run build -- --configuration production'
                }
            }
        }

        // ── 6. Docker Build (parallel) ────────────────────────────────────────
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

        // ── 7. Deploy (main branch only) ──────────────────────────────────────
        stage('Deploy') {
            when {
                anyOf { branch 'main'; branch 'master' }
            }
            steps {
                sh 'docker compose -f $COMPOSE_FILE up -d --remove-orphans'
            }
        }
    }

    post {
        always {
            // Clean up injected secret files so they don't linger on the agent
            sh '''
                rm -f infrastructure/.env .env
                rm -f services/NotificationService/appsettings.Development.json
                # Restore placeholder values in frontend env files
                sed -i "s|rzp_test_[A-Za-z0-9]*|RAZORPAY_KEY_PLACEHOLDER|g" Frontend/src/environments/environment.ts || true
                sed -i "s|rzp_test_[A-Za-z0-9]*|RAZORPAY_KEY_PLACEHOLDER|g" Frontend/src/environments/environment.prod.ts || true
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
        docker build \\
            -f Frontend/Dockerfile \\
            -t grocery/frontend:${env.BUILD_NUMBER} \\
            -t grocery/frontend:latest \\
            Frontend/
    """
}
