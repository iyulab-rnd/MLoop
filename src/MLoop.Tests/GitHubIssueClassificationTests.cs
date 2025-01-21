//using System.Net;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;
//using Xunit;
//using Microsoft.AspNetCore.Mvc.Testing;
//using MLoop.Api;  // MLoop.Api 프로젝트 네임스페이스
//using MLoop.Api.Models;
//using MLoop.Models.Jobs;
//using Microsoft.VisualStudio.TestPlatform.TestHost;
//using MLoop.Models;

//namespace MLoop.Tests
//{
//    public class MLoopIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
//    {
//        private readonly WebApplicationFactory<Program> _factory;
//        private readonly HttpClient _client;

//        public MLoopIntegrationTests(WebApplicationFactory<Program> factory)
//        {
//            _factory = factory;
//            _client = _factory.CreateClient();
//        }

//        [Fact]
//        public async Task 시나리오_생성_데이터업로드_모델훈련_예측_통합테스트()
//        {
//            // 1. 시나리오 생성
//            var scenarioRequest = new CreateScenarioRequest
//            {
//                Name = "test_scenario",
//                MLType = "classification",
//                Tags = new() { "integration", "test" }
//            };
//            var scenarioCreateResponse = await _client.PostAsync(
//                "/api/Scenarios",
//                new StringContent(JsonSerializer.Serialize(scenarioRequest), Encoding.UTF8, "application/json")
//            );
//            scenarioCreateResponse.EnsureSuccessStatusCode();
//            var scenarioResult = await scenarioCreateResponse.Content.ReadAsStringAsync();
//            var scenario = JsonSerializer.Deserialize<ScenarioMetadata>(scenarioResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
//            Assert.NotNull(scenario?.ScenarioId);

//            // 2. 데이터 업로드 (CSV 예시)
//            var csvContent = "Label,Feature1,Feature2\nA,1,2\nB,3,4"; // 간단한 예시
//            var csvBytes = Encoding.UTF8.GetBytes(csvContent);
//            var content = new MultipartFormDataContent
//            {
//                {
//                    new StreamContent(new MemoryStream(csvBytes))
//                    {
//                        Headers =
//                        {
//                            ContentType = new MediaTypeHeaderValue("text/csv")
//                        }
//                    },
//                    "files",
//                    "train.csv"
//                }
//            };
//            var uploadResponse = await _client.PostAsync($"/api/scenarios/{scenario!.ScenarioId}/data", content);
//            uploadResponse.EnsureSuccessStatusCode();

//            // 3. 모델 훈련(Train API 호출)
//            var trainResponse = await _client.PostAsync($"/api/Scenarios/{scenario.ScenarioId}/train", null);
//            trainResponse.EnsureSuccessStatusCode();
//            var trainResultJson = await trainResponse.Content.ReadAsStringAsync();
//            // jobId, status 등이 담김
//            var trainResult = JsonSerializer.Deserialize<TrainJobResponse>(trainResultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
//            Assert.NotNull(trainResult?.jobId);

//            // 4. 학습 Job 완료 대기
//            await WaitForJobCompletionAsync(scenario.ScenarioId, trainResult.jobId!);

//            // 5. 예측 요청( CSV 입력으로 예시)
//            var predictContent = "Feature1,Feature2\n5,6\n7,8";
//            var predictionResponse = await _client.PostAsync(
//                $"/api/scenarios/{scenario.ScenarioId}/predict",
//                new StringContent(predictContent, Encoding.UTF8, "text/csv")
//            );
//            predictionResponse.EnsureSuccessStatusCode();
//            var predictionJson = await predictionResponse.Content.ReadAsStringAsync();
//            var predictionResult = JsonSerializer.Deserialize<PredictResponse>(predictionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
//            Assert.NotNull(predictionResult?.predictionId);

//            // 6. 예측 Job 완료 대기
//            await WaitForJobCompletionAsync(scenario.ScenarioId, predictionResult.predictionId!);

//            // 7. 예측 결과 확인
//            //    /api/scenarios/{scenarioId}/predictions/{predictionId} 에서 결과 확인
//            var getPredictResult = await _client.GetAsync(
//                $"/api/scenarios/{scenario.ScenarioId}/predictions/{predictionResult.predictionId}"
//            );
//            Assert.Equal(HttpStatusCode.OK, getPredictResult.StatusCode);
//        }

//        private async Task WaitForJobCompletionAsync(string scenarioId, string jobId, int timeoutSeconds = 30)
//        {
//            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

//            while (!cts.IsCancellationRequested)
//            {
//                var response = await _client.GetAsync($"/api/scenarios/{scenarioId}/jobs/{jobId}");
//                response.EnsureSuccessStatusCode();

//                var jobJson = await response.Content.ReadAsStringAsync();
//                var jobStatus = JsonSerializer.Deserialize<MLJob>(jobJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

//                if (jobStatus != null && (jobStatus.Status == MLJobStatus.Completed || jobStatus.Status == MLJobStatus.Failed))
//                {
//                    break;
//                }

//                await Task.Delay(1000, cts.Token);
//            }
//        }

//        // 훈련 요청 JSON 바인딩용
//        private class TrainJobResponse
//        {
//            public string? jobId { get; set; }
//            public MLJobStatus status { get; set; }
//        }

//        // 예측 요청 JSON 바인딩용
//        private class PredictResponse
//        {
//            public string? predictionId { get; set; }
//            public string? modelId { get; set; }
//            public string? workflowName { get; set; }
//            public bool isUsingBestModel { get; set; }
//        }
//    }
//}
