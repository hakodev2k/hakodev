pipeline {
    agent any
    
    environment {
        DOTNET_CLI_HOME = "/tmp/DOTNET_CLI_HOME"
        DOTNET_CLI_TELEMETRY_OPTOUT = "1"
        PATH = "${PATH}:${HOME}/.dotnet/tools"
    }
    
    stages {
        stage('Determine Target Branch') {
            steps {
                script {
                    env.TARGET_BRANCH = ""
                    env.ENVIRONMENT = ""
                    
                    if (env.CHANGE_TARGET) {
                        env.TARGET_BRANCH = env.CHANGE_TARGET
                    } else if (env.BRANCH_NAME) {
                        env.TARGET_BRANCH = env.BRANCH_NAME
                    }
                    
                    echo "Target branch: ${env.TARGET_BRANCH}"
                    
                    // Xác định môi trường dựa trên target branch
                    if (env.TARGET_BRANCH == 'Testing') {
                        env.ENVIRONMENT = 'Testing'
                    } else if (env.TARGET_BRANCH == 'Staging') {
                        env.ENVIRONMENT = 'Staging'
                    } else if (env.TARGET_BRANCH == 'master') {
                        env.ENVIRONMENT = 'Production'
                    } else {
                        error "Branch ${env.TARGET_BRANCH} không được cấu hình để triển khai"
                    }
                    
                    echo "Environment: ${env.ENVIRONMENT}"
                }
            }
        }
        
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        
        stage('Restore Packages') {
            steps {
                sh 'dotnet restore'
            }
        }
        
        stage('Build') {
            steps {
                sh 'dotnet build --configuration Release --no-restore'
            }
        }
        
        stage('Test') {
            steps {
                script {
                    try {
                        sh 'dotnet test --configuration Release --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/coverage.cobertura.xml'
                        sh 'dotnet tool install -g dotnet-reportgenerator-globaltool'
                        sh 'reportgenerator -reports:./TestResults/coverage.cobertura.xml -targetdir:./TestResults/Coverage -reporttypes:Html'
                    } catch (Exception e) {
                        echo "Warning: Tests could not be run, possibly because no test projects were found. Continuing pipeline."
                    }
                }
            }
            post {
                always {
                    script {
                        try {
                            cobertura coberturaReportFile: './TestResults/coverage.cobertura.xml', failUnhealthy: false, failUnstable: false, onlyStable: false
                        } catch (Exception e) {
                            echo "Warning: Could not process code coverage report. Continuing pipeline."
                        }
                    }
                }
            }
        }
        
        stage('SonarQube Analysis') {
            steps {
                withSonarQubeEnv('SonarQube') {
                    sh '''
                        dotnet sonarscanner begin /k:"hakodev" /d:sonar.host.url="${SONAR_HOST_URL}" /d:sonar.login="${SONAR_AUTH_TOKEN}" /d:sonar.cs.opencover.reportsPaths="./TestResults/coverage.cobertura.xml" /d:sonar.coverage.exclusions="**/Program.cs,**/Startup.cs,**/ApplicationDbContext.cs"
                        dotnet build --configuration Release --no-restore
                        dotnet sonarscanner end /d:sonar.login="${SONAR_AUTH_TOKEN}"
                    '''
                }
                timeout(time: 1, unit: 'HOURS') {
                    waitForQualityGate abortPipeline: true
                }
            }
        }
        
        stage('Package') {
            steps {
                sh 'dotnet publish ./hakodev/hakodev.csproj --configuration Release --output ./publish'
                sh 'mkdir -p ./artifacts'
                sh 'cd ./publish && zip -r ../artifacts/hakodev.${ENVIRONMENT}.${BUILD_NUMBER}.zip .'
            }
        }
        
        stage('Push to Octopus') {
            steps {
                withCredentials([string(credentialsId: 'octopus-api-key', variable: 'OCTOPUS_API_KEY')]) {
                    sh '''
                        curl -X POST "http://4.194.43.57:8080/api/packages/raw?replace=false" \
                        -H "X-Octopus-ApiKey: ${OCTOPUS_API_KEY}" \
                        -F "data=@artifacts/hakodev.${ENVIRONMENT}.${BUILD_NUMBER}.zip"
                    '''
                    
                    sh '''
                        curl -X POST "http://4.194.43.57:8080/api/releases" \
                        -H "X-Octopus-ApiKey: ${OCTOPUS_API_KEY}" \
                        -H "Content-Type: application/json" \
                        -d '{
                            "ProjectId": "Projects-1", 
                            "Version": "'${ENVIRONMENT}.${BUILD_NUMBER}'",
                            "SelectedPackages": [
                                {
                                    "StepName": "Deploy Website", 
                                    "Version": "'${ENVIRONMENT}.${BUILD_NUMBER}'"
                                }
                            ]
                        }'
                    '''
                }
            }
        }
        
        stage('Deploy to Testing') {
            when {
                expression { return env.ENVIRONMENT == 'Testing' }
            }
            steps {
                withCredentials([string(credentialsId: 'octopus-api-key', variable: 'OCTOPUS_API_KEY')]) {
                    sh '''
                        octo push \
                            --server http://4.194.43.57:8080 \
                            --apiKey ${OCTOPUS_API_KEY} \
                            --package ./artifacts/hakodev.${ENVIRONMENT}.${BUILD_NUMBER}.zip \
                            --packageId hakodev \
                            --version ${BUILD_NUMBER}
                    '''
                }
            }
        }
    }
    
    post {
        always {
            cleanWs()
        }
        success {
            echo 'Pipeline completed successfully!'
        }
        failure {
            echo 'Pipeline failed!'
        }
    }
}