# AES Encryption and Decryption Application

## Overview
This project implements **AES (Advanced Encryption Standard)** — one of the most secure and widely used symmetric encryption algorithms.  
The application allows users to **encrypt and decrypt text paragraphs** using dynamically generated keys and initialization vectors (IV).  

It features a simple and user-friendly **WPF graphical interface**, where users can:
- Input plaintext or encrypted messages
- Choose to encrypt or decrypt
- Save results and cryptographic files automatically

---

## How AES Works
**AES (Advanced Encryption Standard)** is a symmetric key algorithm, meaning it uses the **same key** for both encryption and decryption.  
The process involves multiple rounds of substitutions, permutations, and mathematical transformations on blocks of data.

### Key Points:
- AES operates on 128-bit data blocks
- Supports key sizes of **128, 192, and 256 bits**
- Uses a **key** and an **IV (Initialization Vector)** for secure encryption
- The encrypted output is encoded in **Base64** for readability and storage

---

## Features
 AES Encryption (symmetric key algorithm)  
 Automatic key and IV generation for every encryption session  
 Save encrypted text, key, and IV files in chosen directory  
 Decrypts only with matching key and IV  
 User-friendly GUI made with **WPF (C#)**  
 Status messages and file tracking  

---

## System Requirements
- **Operating System:** Windows 10 or later  
- **IDE/Editor:** Visual Studio or Visual Studio Code  
- **.NET SDK:** Version 6.0 or higher  
- **Language:** C#  
- **UI Framework:** WPF (Windows Presentation Foundation)

---

## How to Run
**Steps:** 
   git clone https://github.com/Jkllen/aescrypt
   
   cd aescrypt 

   dotnet build

   dotnet run

---

## AES performs the following main steps:

**SubBytes:** Byte substitution using S-box transformation

**ShiftRows:** Rows are shifted cyclically

**MixColumns:** Columns are mixed via matrix multiplication

**AddRoundKey:** Each byte is XORed with a round key derived from the main key

These steps are repeated for 10, 12, or 14 rounds depending on the key size.

