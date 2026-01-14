# Multiplayer FPS with Advanced AI & Hybrid Leaderboard (Unity + Photon PUN 2)

A high-fidelity multiplayer First-Person Shooter (FPS) developed in **Unity** using **Photon PUN 2**. This project features a robust networking architecture capable of handling hybrid matches with both Real Players and Intelligent AI Bots, all tracked on a single, unified real-time leaderboard.

![Project Banner] <img width="1919" height="1036" alt="Screenshot 2026-01-14 170101" src="https://github.com/user-attachments/assets/0fe72301-27b9-4533-b181-6c16f27fb7a6" />


<img width="1919" height="1079" alt="Screenshot 2026-01-14 170138" src="https://github.com/user-attachments/assets/9adee972-daa1-4a8a-9fb2-0cf7c7e54950" />
<img width="1919" height="1079" alt="Screenshot 2026-01-14 170152" src="https://github.com/user-attachments/assets/35da0269-f02e-4ce0-af10-3ccfc154f72d" />
<img width="1919" height="1079" alt="Screenshot 2026-01-14 170252" src="https://github.com/user-attachments/assets/baa003c9-98c0-4608-8056-be4e7b1c3094" />
<img width="1919" height="1079" alt="Screenshot 2026-01-14 170318" src="https://github.com/user-attachments/assets/8b9f300f-c1f2-49d7-9585-2731a6afd362" />
<img width="1919" height="1079" alt="Screenshot 2026-01-14 170343" src="https://github.com/user-attachments/assets/b29f381c-2472-4a0a-9306-faa4e6dc62ef" />
<img width="1919" height="1079" alt="Screenshot 2026-01-14 170400" src="https://github.com/user-attachments/assets/513a3b28-7f75-4a48-81d0-9da78fedef76" />
<img width="1919" height="1076" alt="Screenshot 2026-01-14 170503" src="https://github.com/user-attachments/assets/5648584c-1611-4710-bdab-3c384e336b76" />

## 🎮 Key Features

### 1. Hybrid Multiplayer Architecture
* **Unified Ecosystem:** Supports seamless interaction between Human Players (networked clients) and AI Bots (Master Client controlled).
* **Room Management:** Custom lobby system supporting up to 20 entities (Players + Bots) per match.
* **Late-Join Support:** New players receive up-to-date game states (scores, active bots, timer) immediately upon joining.

### 2. Intelligent AI Bot System
* **Persistent Stats (Instantiation Data):** Solved the "Stat Reset" problem. When a bot dies, its Score, Kills, and Name are injected into the respawned instance using `PhotonNetwork.Instantiate` custom data arrays. This ensures bots maintain their leaderboard rank throughout the match.
* **Behavior Tree Logic:** Bots utilize `NavMeshAgent` for pathfinding with state-machine logic for Patrolling, Chasing, and Attacking.
* **Tactical Movement:**
    * **Vaulting:** Bots detect obstacles via Raycasts and perform synchronized vaulting animations.
    * **Combat Spacing:** AI dynamically switches between Sprinting (Chase) and Walking (Combat) based on attack range.
* **Spawn Protection:** Implemented a 3-second invulnerability shield and "Force Heal" logic to prevent spawn-camping and physics initialization errors.

### 3. Unified Real-Time Leaderboard
* **Data Merging Algorithm:** A custom system that aggregates data from two disparate sources: `PhotonNetwork.PlayerList` (Humans) and `FindObjectsOfType<BotController>` (AI).
* **Sorting & Display:** Dynamically sorts all entities by Score (Descending) and displays K/D ratios.
* **Kill Attribution:** The Damage System passes the `AttackerID` via RPC. This ensures that if a Bot kills a Player (or vice versa), the correct entity is awarded the point.

### 4. Advanced Health & Regeneration
* **Auto-Regeneration:** "Battle Royale" style healing. If a player avoids damage for 5 seconds, health regenerates over time.
* **Networked Sync:** Uses `IPunObservable` to smoothly synchronize health values (integers) and regeneration floats across the network.
* **Death Sequences:**
    * **Real Players:** 3-second Third-Person death animation, movement lock, and respawn timer.
    * **Bots:** Physics freeze (Kinematic Rigidbody) to prevent clipping through the floor, followed by a networked object destroy/recycle.

### 5. Weapon & Combat Mechanics
* **Safe RPC Architecture:** Weapon scripts locate the parent `PhotonView` to prevent `NullReferenceExceptions` when shooting from child objects.
* **Ballistics:** Raycast shooting with Recoil, Spread, and Range limits.
* **Feedback:** Networked Muzzle Flashes, Bullet Impact VFX, and Sound Effects.

---

## 🛠️ Technical Implementation Highlights

### The "Persistent Bot" Solution
To keep Bot scores alive after death, we utilize Photon's Instantiation Data. This prevents the stats from resetting to 0 when a new bot object spawns.

```csharp
// BotSpawner.cs - Packing Data
object[] myCustomData = new object[] { oldName, oldScore, oldKills, oldDeaths };
PhotonNetwork.Instantiate(botPrefab.name, pos, rot, 0, myCustomData);

// BotController.cs - Unpacking Data
public void OnPhotonInstantiate(PhotonMessageInfo info) {
    object[] data = info.photonView.InstantiationData;
    // Restore stats immediately before Start()
    score = (int)data[1]; 
}****

The Leaderboard Merge Logic
Combining local AI data with Networked Player data into one sorted list:

C#

// LeaderBoard.cs
List<PlayerData> allPlayers = new List<PlayerData>();

// 1. Add Humans (Networked)
foreach (var p in PhotonNetwork.PlayerList) { allPlayers.Add(new PlayerData(p)); }

// 2. Add Bots (Scene Objects)
foreach (var b in FindObjectsOfType<BotController>()) { allPlayers.Add(new PlayerData(b)); }

// 3. Sort by Score
var sorted = allPlayers.OrderByDescending(x => x.score).ToList();

📦 Setup & Installation
Clone the Repository:

Bash

git clone [https://github.com/yourusername/your-fps-project.git](https://github.com/yourusername/your-fps-project.git)
Open in Unity:

Developed with Unity 2021.3.x (LTS recommended).

Photon Setup:

Create an App ID at Photon Engine.

Paste it into Window > Photon Unity Networking > PUN Wizard.

Resources Folder:

Ensure PlayerPrefab and BotPlayer are located inside the Assets/Resources folder (Required for PhotonNetwork.Instantiate).

🕹️ Controls

Key,Action
"W, A, S, D",Movement
Shift,Sprint
Space,Jump
Mouse Left,Fire
Mouse Right,Aim / Scope
R,Reload
Tab,Leaderboard

🔮 Roadmap
[ ] Team Deathmatch (Red vs Blue) logic.

[ ] Weapon Pickup/Drop system.

[ ] Photon Voice Chat integration.

Developed by VISHNU B
