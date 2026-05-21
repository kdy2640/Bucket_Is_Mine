<!-- HERO (centered banner) -->
<h1 align="center">Miner</h1>
<p align="center"><em>A 3D casual game about digging through deformable terrain, discovering treasures, and earning money from the adventure.</em></p>

<p align="center">
  <a href="https://www.youtube.com/watch?v=NIxopKzcRkQ">
    <img src="https://img.youtube.com/vi/NIxopKzcRkQ/hqdefault.jpg" alt="Watch the Prototype" width="70%">
  </a>
</p>
 
---

## WIP: About the Project
**Miner**는  
풀밭 같은 공간을 자유롭게 파헤치고,  
숨겨진 보물과 자원을 모아 돈으로 환전하며,  
레벨을 클리어하거나 업그레이드를 구매하는  
**3D 캐주얼/시뮬레이션 기반 프로토타입**입니다.

초기 목표는 Marching Cubes 기반의 변형 가능한 지형을 구현하고,  
플레이어가 이를 파내면서 자원을 얻는 **“Digging / Discovering / Collecting Loop”**를 만드는 것입니다.

현재는 실험 단계(WIP)이며,  
게임플레이 루프와 그래픽/지형 시스템을 빠르게 테스트하기 위한 목적의 프로토타입입니다.

---

## Project Status
**Development Status: WIP (Prototype Stage)**  
다음 기능들을 중심으로 구현/정리 중입니다.

---

## Planned Features & Roadmap

### Core Systems
- [Δ] **Marching Cubes 기반 지형 생성 및 청크 시스템**
- [Δ] **지형 실시간 파괴 / 변형 기능**
- [ ] **보물/자원 오브젝트 스폰 및 수집 시스템**
- [ ] **보물/자원 → 돈 환전 루프 구현**
- [ ] **간단한 업그레이드 시스템**

### Player Interaction
- [Δ] **지형을 파거나 긁어내는 도구 시뮬레이션**
- [ ] **수집/자석 계열 기능**
- [ ] **파내기 & 수집 감각 개선**

### Content
- [ ] **보물 종류 다양화 (가벼운 것/무거운 것/특수 아이템)**
- [ ] **레벨(맵) 테마 추가**
- [ ] **지형 형태/패턴 다양화**

### Visual / FX
- [Δ] **Marching Cubes 시각 효과 개선**
- [ ] **흙/잔디 파편 및 파티클 효과**
- [ ] **수집/흡입 VFX**

### Technical Experiments
- [ ] **LOD/최적화**
- [ ] **GPU 기반 Marching Cubes 실험**
- [ ] **Object Pooling 정리**

---

## Current Progress (Prototype)
- Unity 기반 Marching Cubes 실험 코드  
- 3D 모델 배치 구조 설계 중  
- 보물/자원 오브젝트 분류 및 수집 컨셉 테스트 중  
- 컨트롤/카메라 실험 단계  
- 기본 루프 설계 완료 (Treasure → Money → Upgrade)

---

## Development Purpose
이 프로젝트는  
**Marching Cubes + 캐주얼 루프 + 시뮬레이션 최적화**의  
연구 및 실험 목적을 겸한 프로토타입입니다.

특히 다음을 중점으로 삼고 있습니다:

- 변형 가능한 3D 지형과 상호작용하는 캐주얼 게임 구조 실험  
- 물리/메시 생성/최적화에 대한 테스트  
