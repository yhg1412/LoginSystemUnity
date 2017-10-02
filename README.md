Login and Chat system based on Unity

LoginSystemUnity is a secured multiplayer chat system with login intergration. The system is composed of three parts: a client made with Unity3D, a dedicated server under .net 4.5.2, and a MySQL database.

The encryption schema consists two parts:
1. Message transportation encryption imitated SSL(Secure Socket Layer), it uses RSA2048 for key exchange and uses AES for actual data transportaion. It aslo imitated digital signature with SHA256 to check authentic public key.
2. Secured password storage in MySQL database is met by using SHA256 Hash function and random salt combintion.

Ohter information: It was originally meant to be part of a multiplayer game I designed. Yet I ended up with this system.

The below are detailed explaination in Chinese. (English version coming soon)

加密方案 (Encryption Schema)
1数据传输加密方案：
	1. 生成第一对RSA公钥pubcrt，私钥pricrt，用于模拟本系统的“根证书”。将私钥pricrt保存在服务器安全容器内。将公钥pubcrt硬编码进客户端程序中，模拟作为安装在用户端的根证书。
	2. 服务器得到一个连接请求时，生成一对RSA秘钥：公钥pubkey，私钥prikey。将pubkey作SHA256散列得到信息摘要Digest，将Digest用私钥pricrt加密生成数字签名signature。服务器将（pubkey + signature）发给客户端。
	3. 客户端用pubcrt解密signature，得到摘要Digest，同时将收到的pubkey也用SHA256散列得到摘要Digest’，比对两个信息摘要Digest与Digest’，若内容一致，则说明：若客户端相信pricrt的持有者是server，那么得到的pubkey一定是server发送的并且没有被篡改过。
	4. 客户端生成一个对称AES秘钥AesKey，用第三步中收到的pubkey加密对称秘钥AesKey，将加密后的AesKey发送到服务器。
	5. 服务器收到加密的AesKey后，用prikey解密该信息，得到对称秘钥AesKey。
	6. 在本次连接中，服务器与客户端均使用对称秘钥AesKey来进行数据交换。

2用户数据保存方案：
	注册时：
	1. 客户端将密码作AES256散列，得到密码摘要Digest1。
	2. 建立上述安全连接后，将Digest1发送给服务器。
	3. 服务器接受到Digest1后，对于每个新的用户，生成一个随机串Salt。
	4. 服务器将Digest1串与Salt串拼接后再进行AES256散列得到摘要Digest2。
	5. 服务器将用户名，Digest2，salt一并存入数据库中。
	登录时：
	1. 客户端将密码作AES256散列，得到密码摘要Digest1。
	2. 建立上述安全连接后，将用户名与Digest1发送给服务器。
	3. 服务器从数据库中取出该用户的Digest2与Salt。将Digest1与Salt按照之前的方式进行拼接并进行AES256散列得到摘要Digest2’。
	4. 服务器将Digest2’与Digest2进行比对，若比对成功，则登陆成功

3自动登录方案：
    1.	用户填写密码，客户端随机生成一个salt值（注意这个salt只是防止中间人拦截到原始的password的加密串），用公钥把 (salt + password)加密，设置首次登陆的参数，发送到服务器；
    2.	服务器检查参数，发现是首次登陆，则服务器用私钥解密，得到password（抛弃salt值），验证，如果通过，则随机生成一个salt值，并把salt值保存起来（保存到缓存里，设置7天过期），然后用公钥把(salt + 用户名)加密，返回给客户端。
    3.	客户端保存服务器返回的加密串，完成登陆。
    4.	客户端下次自动登陆时，把上次保存的加密串直接发给服务器，并设置二次登陆的参数。
    5.	服务器检查参数，发现是二次登陆，用私钥解密，得到salt + 用户名，然后检查salt值是否过期了（到缓存中查找，如果没有，即过期），如果过期，则通知客户端，让用户重新输入密码。如果没有过期，再验证密码是否正确。如果正确，则通知客户端登陆成功。
    6.	如果发现某帐户异常，可以直接清除缓存中对应用户的salt值，这样用户再登陆就会失败。同理，如果某木马大规模窃取到了大量的用户本地加密串，那么可以把缓存中所有用户的salt都清除，那么所有用户都要重新登陆。注意用户的密码不用修改。
    7.	第2步中服务器生成的salt值，可以带上用户的mac值，os版本等，这样可以增强检验。
 
