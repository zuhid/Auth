const NAMES = [
  'Alice', 'Bob', 'Carol', 'David', 'Eva', 'Frank', 'Grace', 'Henry', 'Isla', 'James', 'Karen', 'Liam', 'Mia', 'Noah', 'Olivia', 'Paul', 'Rachel', 'Sam', 'Tara', 'Zoe', // English
  'Carlos', 'María', 'José', 'Ana', 'Luis', 'Rosa', 'Miguel', 'Elena', 'Pablo', 'Carmen', 'Juan', 'Sofía', 'Diego', 'Lucía', 'Valentina', 'Javier', 'Gabriela', 'Fernando', 'Camila', // Spanish
  'Pierre', 'Marie', 'Jean', 'Claire', 'François', 'Isabelle', 'Philippe', 'Céline', 'Laurent', 'Nathalie', 'Michel', 'Amélie', 'Luc', 'Fleur', 'Antoine', 'Sophie', 'René', 'Monique', // French
  'Hans', 'Greta', 'Klaus', 'Ingrid', 'Werner', 'Helga', 'Fritz', 'Ursula', 'Otto', 'Brigitte', 'Heinz', 'Dieter', 'Monika', 'Wolfgang', 'Rolf', 'Hildegard', 'Günter', 'Sigrid', // German
  'Akira', 'Yuki', 'Kenji', 'Hana', 'Takeshi', 'Sakura', 'Hiroshi', 'Aiko', 'Ryota', 'Yoko', 'Daisuke', 'Naomi', 'Taro', 'Emi', 'Haruki', 'Midori', 'Kazuki', 'Saki', 'Ren', 'Yuna', // Japanese
  'Wei', 'Lin', 'Fang', 'Yang', 'Jing', 'Ming', 'Xiao', 'Hui', 'Lei', 'Yan', 'Ping', 'Qiang', 'Hong', 'Mei', 'Bo', 'Ling', 'Xin', 'Zhen', 'Hao', 'Yun', // Chinese
  'Priya', 'Raj', 'Anjali', 'Vijay', 'Deepa', 'Arun', 'Sunita', 'Ravi', 'Pooja', 'Suresh', 'Ananya', 'Kiran', 'Meera', 'Vikram', 'Divya', 'Arjun', 'Kavya', 'Rohit', 'Nisha', 'Sanjay', // Indian
  'Ahmed', 'Fatima', 'Omar', 'Layla', 'Hassan', 'Nour', 'Ali', 'Yasmin', 'Khalid', 'Tariq', 'Rania', 'Karim', 'Yusuf', 'Zara', 'Ibrahim', 'Salma', 'Leila', 'Hamid', 'Samira', 'Walid', // Arabic
  'Marco', 'Giulia', 'Luca', 'Francesca', 'Matteo', 'Lorenzo', 'Chiara', 'Davide', 'Alessia', 'Andrea', 'Elena', 'Riccardo', 'Giovanni', 'Martina', 'Stefano', 'Roberto', 'Antonio', // Italian
  'João', 'Pedro', 'Miguel', 'Sofia', 'Rui', 'Inês', 'Nuno', 'Catarina', 'Tiago', 'Mariana', 'Diogo', 'Beatriz', 'Filipe', 'Marta', 'Henrique', 'Joana', 'André', 'Carla', 'Sandra', // Portuguese
  'Alexei', 'Natasha', 'Dmitri', 'Olga', 'Ivan', 'Tatiana', 'Nikolai', 'Irina', 'Vladimir', 'Svetlana', 'Mikhail', 'Ekaterina', 'Sergei', 'Marina', 'Pavel', 'Anastasia', 'Boris', // Russian
  'Minho', 'Jiyeon', 'Seojun', 'Hyeji', 'Dohyun', 'Yuna', 'Junhyuk', 'Soyeon', 'Jaehyun', 'Minji', 'Taehyung', 'Suji', 'Woojin', 'Eunji', 'Sanghyun', 'Bora', 'Jihoon', 'Chaewon', // Korean
  'Erik', 'Astrid', 'Lars', 'Sigrid', 'Björn', 'Freya', 'Magnus', 'Ingrid', 'Sven', 'Gunnar', 'Ragnhild', 'Harald', 'Gudrun', 'Leif', 'Thyra', 'Olaf', 'Solveig', 'Brynja', // Scandinavian
  'Amara', 'Kofi', 'Kwame', 'Fatou', 'Seun', 'Adaeze', 'Chidi', 'Nkechi', 'Emeka', 'Abena', 'Yaw', 'Adjoa', 'Kweku', 'Akosua', 'Kojo', 'Efua', 'Nana', 'Esi', 'Ama', 'Osei', // West African
  'Mehmet', 'Ayşe', 'Mustafa', 'Fatma', 'Zeynep', 'Hüseyin', 'Emine', 'Ömer', 'Hatice', 'İbrahim', 'Elif', 'Ahmet', 'Meryem', 'Yusuf', 'Süleyman', 'Şerife', 'Hasan', 'Selma', // Turkish
];

function ascii(name) {
  return name.normalize('NFKD').replace(/[^A-Za-z]/g, '') || 'user';
}

function pick(arr) {
  return arr[Math.floor(Math.random() * arr.length)];
}

export const random = {
  string(length = 8) {
    return pick(NAMES);
  },

  number(min = 1000, max = 9999) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  },

  phone() {
    const area = Math.floor(Math.random() * 900) + 100;
    const mid = Math.floor(Math.random() * 900) + 100;
    const end = Math.floor(Math.random() * 9000) + 1000;
    return `${area}-${mid}-${end}`;
  },

  // Email addresses are ASCII-folded (Ömer -> Omer): mail to non-ASCII addresses never reaches
  // MailHog, so a random accented name made the email-dependent tests fail intermittently.
  email() {
    return `${ascii(pick(NAMES))}${Date.now()}@${ascii(pick(NAMES))}.com`;
  },

  name() {
    return pick(NAMES);
  },
};
