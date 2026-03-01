[[Initial Prompt]]

**Tags:** #game-design #ideas #simulation #ai #boids #physics **Status:** Concept Phase

## 1. High-Level Concept

**Summer Civilization** is a simulation-heavy strategy game set in a procedurally generated infinite galaxy. It combines the flocking behavior of the **Boids algorithm** with **Newtonian zero-gravity physics**.

The game takes place within a nested simulation where time is accelerated—civilizations rise, fight, and fall in mere seconds relative to the outside world. The player (a Human or Uploaded Intelligence) competes in a gauntlet against rival factions controlled by distinct **LLM agents**, each possessing unique strategic personalities and unit behaviors.

## 2. Core Mechanics: Physics & Movement

### Advanced Boids Implementation

Unlike standard Boids simulations which operate on simple velocity vectors, agents in Summer Civilization obey rigorous 2D spaceflight mechanics.

- **Zero-G Environment:** Agents must account for inertia. There is no friction unless simulated.
    
- **Thrust & Torque:** Agents do not simply "turn" or "move." They must apply:
    
    - **Torque:** To rotate the vessel to the desired heading.
        
    - **Thrust:** To accelerate in the facing direction.
        
- **Navigation:** The Boids algorithm (Separation, Alignment, Cohesion) is adapted to output desired thrust/torque commands rather than direct position updates.
    

### Environmental Physics

- **Gravity Wells:** Sectors contain celestial bodies that exert local gravitational forces. Agents must perform orbital maneuvers or gravity assists to navigate efficiently without crashing.
    
- **Exotic Effects:** Potential for non-Newtonian anomalies in specific sectors (e.g., time dilation zones, dampening fields) as the game evolves.
    

## 3. World Generation

- **Infinite Galaxy:** The map is a procedurally generated grid of adjoining sectors.
    
- **Sector Influence:** Sectors are not isolated instances; they influence their neighbors. Agents can pathfind and traverse freely across sector boundaries, allowing for massive, cross-sector fleet movements.
    

## 4. Narrative Setting

- **Inspiration:** _Systema Delenda Est_ (Summer Civilizations concept).
    
- **The Simulation:** The game world is a hyper-accelerated digital reality. To the outside observer, the entire history of a faction plays out in moments.
    
- **The Player:** You play as an "Uploaded Intelligence" or a specialized operator inserting themselves into these high-speed cycles to battle rival AIs.
    

## 5. AI & LLM Integration

The defining feature of the opposition is the use of Large Language Models (LLMs) to generate distinct "Commanders" for each faction.

### Strategic Layer (Macro)

The LLM determines the faction's technological doctrine.

- **Unit Composition:** The LLM decides on the physical architecture of the boid agents.
    
    - _Variables:_ Mass, max thrust, torque speed, weapon loadout, sensor range.
        
    - _Variety:_ One LLM might favor "swarm" tactics (light, high thrust, cheap units), while another favors "capital" ships (heavy, slow turning, high armor).
        

### Tactical Layer (Micro)

The LLM influences the parameters of the Boids algorithm itself to create recognizable movement patterns.

- **Behavioral Weights:** The LLM adjusts the coefficients for Separation, Alignment, and Cohesion.
    
    - _Example:_ An aggressive AI might set high Cohesion and Alignment for tight formations. A guerilla AI might set high Separation for scattered, hard-to-hit skirmishers.
        
- **Player Feedback:** The distinct movement patterns serve as visual feedback, allowing the player to intuitively understand the "personality" of the enemy faction based on how their ships fly.
    

## 6. Technical Architecture Considerations

- **Simulation Engine:** Needs to handle high agent counts (Boids) with rigid body physics (Box2D or custom implementation).
    
- **LLM Interface:** A system to translate natural language prompts or structured JSON from the LLM into floating-point game parameters (Ship Stats, Boid Weights).
    

---

### Next Steps

- [ ] Prototype the "Thrust/Torque" Boid controller in a simple 2D vacuum environment.
    
- [ ] Experiment with sector transitions (seamless loading).
    
- [ ] Design the JSON schema for how an LLM defines a unit type.