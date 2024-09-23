
## 학습

1. 폴더 생성 `D:\mloop-folder\scenario_1`
2. `meta.json` 생성 (시나리오 정보)

```
{
  "type": "Recommendation"
}
```

3. `data.csv` 파일 생성 (학습 데이터)

```
userId	movieId	rating
1	1	4
1	3	4
1	6	4
1	47	5
1	50	5
1	70	3
1	101	5
1	110	4
1	151	5
1	157	5
1	163	5
1	216	5
1	223	3
1	231	5
...
```

4. `/train1` 폴더 생성 (아무이름)
5. `action.json` 파일 생성

```
{
}
```
빈 형식으로 저장하는 경우 자동으로 Default Action 으로 업데이트 되고 학습이 시작됩니다.

`state.json` // 학습 상태 파일
`console.log` // 학습 로그
`result.json` // 결과 메트릭
`/Model` // 결과 모델

## 예측

1. `/inputs` 폴더 생성 (아무이름)
2. `input_*.csv` 파일 생성

```
userId	movieId	rating
1	1	
1	3	
1	6	
1	47	
1	50	
1	70	
1	101	
1	110	
```
예측할 열을 비우고 작성합니다.
저장하면 5초 이벤트 디바운스후 예측이 시작됩니다.
`input_1-predicted.csv` // 학습 결과 파일