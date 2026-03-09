# THRUSTERS - analiza Assembly-CSharp

## 1) Zakres i co znaleziono

Przeanalizowane pliki z `ZDEKOMPILOWANE Assembly-CSharp` zwiazane z napedem:

- `EPC_SCThruster.cs`
- `EPC_SCGimbalThruster.cs`
- `SCTypeThruster.cs`
- `SCThrusterElectric.cs`
- `SCTypeFuelThruster.cs`
- `SCThrusterSolidFuel.cs`
- `SCThrusterFan.cs`
- `EPC_ThrusterBlow.cs`
- `SCTick_Thruster_*.cs` (Init, InPort, Electric, Fuel, Fan, Forces, SolidFuel, SpaceAtmosphereWater, Heat, Ignition)
- `SCTick_GimbalThruster.cs`
- `SCTick_ValueAccelerator.cs`

Nie znaleziono literalnego napisu `Small Electric Thruster` w samych plikach C#.

Wniosek:

- Konkretne warianty itemow (np. Small/Medium/Large) sa najpewniej trzymane w danych prefabow Unity (serializowane assety), a nie jako stale tekstowe w kodzie C#.
- Kod C# zawiera logike i pola parametrow, ale nie kompletny katalog nazw + wartosci per kazdy prefab.

## 2) Typy silnikow i elementow napedu

Z kodu wynika, ze sa wspierane takie tryby/odmiany:

- Thruster elektryczny: ma `SCThrusterElectric._maxPowerConsumptionPerSec`.
- Thruster paliwowy: ma `SCTypeFuelThruster._maxFuelConsumptionPerSecond`.
- Thruster solid-fuel: ma `SCThrusterSolidFuel._solidFuelTime` i zaplon.
- Gimbal thruster: `SCTypeGimbalThruster` + osobny tick sterowania kanalami.
- Fan thruster: `SCThrusterFan` (obrot smigla i wektorowanie przez ring/circle).
- Blow thruster effect: `SCThrusterBlow` (dmuchanie/oddzialywanie lokalne).

Dodatkowa wskazowka z audio (`AudioID.cs`): profile `Thruster_001` ... `Thruster_005` (Low/Med/High/Max), co potwierdza wiele wariantow thrusterow.

## 3) Domyslne wartosci w `EPC_SCThruster` (fallback, nie per konkret prefab)

Pola z domyslnymi inicjalizacjami:

- `_maxForce = (0, -500000, 0)`
- `_efficiencyInSpace = 1.0`
- `_efficiencyInAtmosphere = 1.0`
- `_efficiencyInWater = 0.0` (brak inicjalizatora, domyslnie 0)
- `_accelerationTime = 1.0`
- `_gimbalLinear = 0.5`
- `_gimbalAngular = 1.0` stopnia (potem konwertowane do radianow)
- `_particleSystemMinVelocity = 5.0`
- `_particleSystemMaxVelocity = 15.0`
- `_fanRotationSpeed = (0, 0, 12)`
- `_fanCircleRotationMin = (0, 0, -15)`
- `_fanCircleRotationMax = (0, 0, 15)`
- `_fanCircleAccelerator = 1.0`
- `_fanCircleForceAngleMultiplier = 5.7`
- `_maxFuelConsumptionPerSecond = 0.0` (brak inicjalizatora, domyslnie 0)
- `_explosionFuelAmount = 5.0`
- `_fuelReactivityThreshold = 1.0`
- `_fuelReactivitySpeed = 0.25`
- `_powerConsumptionPerSec = 20.0`
- `_maxHeatEffect = 0.7`
- `_solidFuelTime = 0.0` (brak inicjalizatora, domyslnie 0)
- `_solidFuelRequiredIgnitionPower = 100.0`

Dodatkowe:

- `EPC_ThrusterBlow._blowStrength = 0.6`
- `EPC_SCGimbalThruster._batteryCapacity = 500.0`

## 4) Jak liczona jest moc/ciag w runtime (kolejnosc tickow)

W uproszczeniu (na klatke):

1. `SCTick_Thruster_Init`
   - ustawia startowo:
   - `frameEfficiency = 1`
   - `frameEfficiencyPowerMultiplier01 = 1`

2. `SCTick_Thruster_InPort` (elektryczne wejscie sterujace)
   - bierze wartosc z portu mocy `ownPorts[0]`
   - reverse toggle moze odwrocic znak
   - clamp do `[0..1]` albo `[-1..1]` gdy bidirectional
   - mnozy przez `frameEfficiency`

3. `SCTick_GimbalThruster` (dla gimbali)
   - wybiera kanal 0..5 i mnozy `frameEfficiency` przez wartosc kanalu
   - jesli brak aktywnego kontrolera w zasiegu 100 m: `frameEfficiency = 0`

4. `SCTick_Thruster_Fuel` (jesli thruster paliwowy)
   - zuzycie/klatke: `maxFuelConsumptionPerSecond / 60`
   - realne zuzycie skaluje sie z `abs(frameEfficiency)`
   - brak paliwa => `frameEfficiency = 0`
   - parametry paliwa modyfikuja wydajnosc (`performance`)

5. `SCTick_Thruster_Electric` (jesli thruster elektryczny)
   - `consumptionPerSec = abs(frameEfficiency) * maxPowerConsumptionPerSec`
   - gdy brak zasilania: `frameEfficiency = 0`

6. `SCTick_Thruster_SpaceAtmosphereWater`
   - mnoznik medium:
   - `lerp(lerp(effSpace, effAtm, atmosphereDensity), effWater, min(1, underwaterMeters*10))`
   - wynik mnozy `frameEfficiencyPowerMultiplier01`

7. `SCTick_Thruster_Accelerator` + `SCTick_ValueAccelerator`
   - target idzie do `SCValueAccelerator._targetValue`
   - biezaca wartosc to `SCValueAccelerator._acceleratorValue`
   - krok zmian: `delta = _speed * (1/60)`
   - dla thrustera: `_speed = 1 / _accelerationTime`

8. `SCTick_Thruster_Forces`
   - finalna sila dodawana do statku tylko gdy `abs(frameEfficiency * frameEfficiencyPowerMultiplier01)` > epsilon
   - wzor:

```text
forceVector = rotate(localRotation, (frameEfficiency * frameEfficiencyPowerMultiplier01) * maxForce)
```

## 5) Fan / rotor / wektorowanie

`SCTick_Thruster_Fan`:

- Wejscie wektorowania bierze z `ownPorts[2]`, clamp `[-1, 1]`.
- `circleRotation1` dochodzi do targetu z predkoscia:
  - `step = fanCircleAccelerator * (1/60)`
- Obraca element ring/circle miedzy `fanCircleRotationMin` i `fanCircleRotationMax`.
- Przelicza wektor sily `maxForce` przez dodatkowy kat i `fanCircleForceAngleMultiplier`.
- Predkosc obrotu wirnika (wizualnie) skaluje sie tak:

```text
fanRotationStep ~ fanRotationSpeed * sign(frameEfficiency) * sqrt(abs(frameEfficiency)) * (1/60)
```

## 6) Solid fuel

`SCTick_Thruster_SolidFuel`:

- Faza zaplonu: `ignition01` rosnie/spada o `1/60` na klatke (wymaga zasilania).
- Przy `ignition01 >= 1` ustawia `remainingTime = solidFuelTime`.
- Potem odlicza `remainingTime -= 1/60`.
- Ciag jest binarny:
  - `frameEfficiency = 1` gdy `remainingTime > 0`
  - `frameEfficiency = 0` gdy koniec paliwa stalego

## 7) Heat i wizualne zalezne od mocy

- Heat efekt: `pow(saturate(abs(frameEfficiency) * multiplier), 0.4)` \* `maxHeatEffect`.
- `ThrusterHeatArea._temperature = 2000` (na serwerze, gdy aktywny), inaczej `0`.
- Ignition brightness: `abs(frameEfficiency) * 30 + 1.5`.
- Light color: lerp(minColor, maxColor, abs(frameEfficiency)\*multiplier).
- Sound loudness: skaluje sie od efektywnej mocy, linear lub nieliniowo zaleznie od typu thrustera.

## 8) Co to znaczy dla moda sterowania moca

Najwazniejsze dzwignie pod przyszly system:

- Sterowanie wejscia mocy: port `ownPorts[0]` (plus reverse).
- Ograniczanie mocy globalnie/per silnik: modyfikacja `SCTypeThruster._frameEfficiency` lub `SCValueAccelerator._targetValue`.
- Limiter zalezny od medium: modyfikacja `frameEfficiencyPowerMultiplier01` po `SpaceAtmosphereWater`.
- Overclock/underclock elektryki: `SCThrusterElectric._maxPowerConsumptionPerSec` (lub runtime clamp przed zuzyciem).
- Soft-start/response tuning: `SCValueAccelerator._speed` (czyli posrednio `accelerationTime`).

## 9) Brakujace dane dla "Small Electric Thruster"

Nie da sie z samych plikow C# jednoznacznie wylistowac "Small Electric Thruster" + jego unikalnych wartosci per prefab.

Powod:

- Nazwa i konkretne liczby per wariant siedza w serializowanych prefabach Unity (inspector data), a nie jako stale w tych klasach.

Jak to uzupelnic przy nastepnym kroku:

- Odczyt runtime z `SCManagedPrefabReference` / `SCPrefab` + komponentow `SCTypeThruster`, `SCThrusterElectric`, `SCTypeFuelThruster`, `SCThrusterFan` dla konkretnych encji.
- Albo eksport danych prefabow z assetow gry (poza samym dekompilowanym C#).

To pozwoli zrobic tabele typu: `Small Electric Thruster -> maxForce, powerConsumption, accelerationTime, ...`.
