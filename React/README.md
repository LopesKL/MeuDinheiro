# 🚀 Framework React

Framework React completo baseado em Vite, Ant Design e React Router, com sistema de autenticação, rotas protegidas, formulários dinâmicos e componentes reutilizáveis.

## 📋 Tecnologias Utilizadas

- **React 19** - Biblioteca JavaScript para construção de interfaces
- **Vite** - Build tool e dev server
- **Ant Design 6** - Biblioteca de componentes UI
- **React Router DOM 6** - Roteamento
- **Axios** - Cliente HTTP
- **Styled Components** - Estilização
- **Day.js** - Manipulação de datas
- **Recharts** - Gráficos
- E outras dependências (ver `package.json`)

## 🏗️ Arquitetura

### Estrutura de Pastas

```
src/
├── assets/          # Arquivos estáticos (imagens, ícones)
├── components/      # Componentes reutilizáveis
│   ├── Data/        # Componentes de dados (Form, Table, FileUpload)
│   ├── Layout/      # Componentes de layout (Card)
│   ├── UI/          # Componentes UI base (Loading, Modal, ErrorBoundary)
│   └── inputs/      # Componentes de input organizados por tipo
├── helpers/         # Funções utilitárias
├── hooks/           # Hooks customizados
├── pages/           # Páginas/views da aplicação
├── routes/          # Configuração de rotas
├── services/        # Serviços de API
├── styles/          # Estilos globais e tema
├── App.jsx          # Componente raiz
└── index.jsx        # Entry point
```

### Fluxo de Dados

1. **Autenticação:**
   - Login → API → localStorage → Context → Rotas protegidas
   - Logout → limpa localStorage → Context → redireciona para SignIn

2. **Rotas:**
   - Rotas definidas em `routes/routes.jsx` com lazy loading
   - Wrapper `RouteWrapper` verifica autenticação e roles
   - Geração dinâmica de rotas com `generateRoutes()`

3. **Componentes:**
   - Props drilling mínimo
   - Context para estado global (auth)
   - Hooks para lógica reutilizável

## 🚀 Como Começar

### Instalação

```bash
# Instalar dependências
npm install

# Iniciar servidor de desenvolvimento
npm run dev

# Build para produção
npm run build

# Preview do build
npm run preview
```

### Variáveis de Ambiente

Crie um arquivo `.env` na raiz do projeto:

```env
VITE_API_URL=https://api.example.com/
VITE_USE_MOCK_AUTH=true  # true para usar credenciais mockadas, false para usar API real
```

### 🔐 Credenciais Mockadas (Desenvolvimento)

O sistema possui credenciais mockadas para desenvolvimento. Use qualquer uma das seguintes:

| Usuário | Senha | Roles |
|---------|-------|-------|
| `admin` | `admin123` | Admin |
| `user` | `user123` | User |
| `teste` | `teste123` | User, Admin |

**Nota:** Para desabilitar o modo mock e usar a API real, defina `VITE_USE_MOCK_AUTH=false` no arquivo `.env`.

## 📖 Guia de Uso

### Adicionar uma Nova Rota

1. Edite `src/routes/routes.jsx`:

```javascript
export const defaultRoutes = [
  // ... rotas existentes
  {
    key: '/nova-rota',
    icon: <IconOutlined />,
    label: 'Nova Rota',
    element: lazy(() => import('@pages/NovaRota')),
    roles: [], // Array de roles permitidos (opcional)
  },
];
```

2. Crie a página em `src/pages/NovaRota/index.jsx`:

```javascript
const NovaRota = () => {
  return <div>Nova Rota</div>;
};

export default NovaRota;
```

### Criar um Novo Componente

1. Crie o componente em `src/components/`:

```javascript
// src/components/MeuComponente.jsx
const MeuComponente = ({ prop1, prop2 }) => {
  return <div>Meu Componente</div>;
};

export default MeuComponente;
```

2. Exporte no `index.jsx` correspondente:

```javascript
// src/components/index.jsx
export { default as MeuComponente } from './MeuComponente';
```

### Adicionar um Novo Tipo de Input

1. Crie o componente Base em `src/components/inputs/tipo/BaseMeuInput.jsx`:

```javascript
import { Input } from 'antd';

const BaseMeuInput = ({ ...props }) => {
  return <Input {...props} />;
};

export default BaseMeuInput;
```

2. Crie o Wrapper em `src/components/inputs/tipo/MeuInput.jsx`:

```javascript
import { Form } from 'antd';
import BaseMeuInput from './BaseMeuInput';

const MeuInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseMeuInput {...props} />
    </Form.Item>
  );
};

export default MeuInput;
```

3. Exporte no `index.js` da pasta:

```javascript
// src/components/inputs/tipo/index.js
export { default as BaseMeuInput } from './BaseMeuInput';
export { default as MeuInput } from './MeuInput';
```

4. Adicione ao DynamicForm em `src/components/Data/Form.jsx`:

```javascript
case 'meu-tipo':
  return <Inputs.MeuInput {...commonProps} />;
```

### Usar o DynamicForm

```javascript
import { DynamicForm } from '@components/Data';

const formConfig = [
  {
    columns: 2, // Número de colunas
    questions: [
      {
        type: 'text',
        id: 'nome',
        label: 'Nome',
        placeholder: 'Digite o nome',
        required: true,
      },
      {
        type: 'email',
        id: 'email',
        label: 'Email',
        required: true,
      },
    ],
  },
];

const handleSubmit = (values) => {
  console.log('Valores:', values);
};

<DynamicForm formConfig={formConfig} onSubmit={handleSubmit} />
```

### Usar o PaginatedTable

```javascript
import { PaginatedTable } from '@components/Data';
import { useRef } from 'react';

const tableRef = useRef();

const fetchData = async (page, pageSize, sorterField, sortOrder) => {
  // Chamada à API
  const response = await Api.get('/dados', {
    params: { page, pageSize, sorterField, sortOrder },
  });
  
  return {
    data: response.data.items,
    total: response.data.total,
  };
};

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', sorter: true },
  { title: 'Nome', dataIndex: 'nome', key: 'nome', sorter: true },
];

const actions = [
  {
    label: 'Editar',
    icon: <EditOutlined />,
    onClick: (record) => console.log('Editar:', record),
  },
];

<PaginatedTable
  ref={tableRef}
  fetchData={fetchData}
  columns={columns}
  actions={actions}
  initialPageSize={10}
/>
```

## 👤 Sistema de Usuário

### Autenticação

O sistema de autenticação usa Context API e localStorage:

- **Login:** Chama `signIn({ username, password })` do hook `useAuth()`
- **Logout:** Chama `signOut()` do hook `useAuth()`
- **Persistência:** Dados salvos no localStorage com prefixo `framework:`

### Controle de Roles

1. Defina roles em `src/helpers/roles.jsx`:

```javascript
export const roles = {
  roleAdmin: 'Admin',
  roleUser: 'User',
};
```

2. Configure roles nas rotas:

```javascript
{
  key: '/admin',
  label: 'Admin',
  element: lazy(() => import('@pages/Admin')),
  roles: ['Admin'], // Apenas Admin pode acessar
}
```

3. O `RouteWrapper` verifica automaticamente se o usuário tem o role necessário.

## 🎨 Estilos e Tema

### Cores

As cores estão definidas em `src/styles/colors.js`:

```javascript
export const colors = {
  primary: "#000",
  secondary: "#52c41a",
  // ...
};
```

### Estilos Globais

Os estilos globais estão em `src/styles/global.jsx` usando styled-components.

### Tema do Ant Design

O tema é configurado no `App.jsx` através do `ConfigProvider`:

```javascript
<ConfigProvider
  locale={ptBR}
  theme={{
    token: {
      colorPrimary: colors.primary,
      // ...
    },
  }}
>
```

## 📝 Padrões do Projeto

### Nomenclatura

- **Componentes:** PascalCase (ex: `TextInput.jsx`)
- **Hooks:** camelCase começando com "use" (ex: `useAuth.jsx`)
- **Helpers/Utils:** camelCase (ex: `helper.js`)
- **Arquivos de índice:** `index.jsx` ou `index.js`

### Estrutura de Componentes

- Cada componente deve ter um arquivo próprio
- Componentes Base e Wrapper separados para inputs
- Exportações centralizadas em `index.jsx`

### Performance

- Uso de `React.memo()` para componentes que não precisam re-renderizar frequentemente
- `useMemo()` e `useCallback()` para evitar recriações desnecessárias
- Lazy loading de componentes/páginas
- Code splitting com Vite (manual chunks)

## 🔧 Configuração do Vite

O `vite.config.js` está configurado com:

- **Aliases:** `@`, `@components`, `@pages`, `@styles`, `@services`
- **Otimizações:** Code splitting, tree shaking
- **Build:** Manual chunks para React e Ant Design

## 🐛 Tratamento de Erros

- **ErrorBoundary:** Captura erros de renderização
- **Try/catch:** Em todas as funções assíncronas
- **Notificações:** Ant Design `notification` para feedback ao usuário
- **API Errors:** Tratamento centralizado em `useExceptionNotification()`

## 📚 Componentes Principais

### DynamicForm

Formulário dinâmico baseado em configuração JSON. Suporta múltiplos tipos de input e validação automática.

### PaginatedTable

Tabela paginada com busca no backend, ordenação, ações customizáveis e seleção de linhas.

### Card

Wrapper customizado do Card do Ant Design com variantes e tamanhos.

### Modal

Wrapper simplificado do Modal do Ant Design com footer customizado.

## 🚧 Próximos Passos (Evolução Futura)

### Sistema de Preferências do Usuário

Estrutura preparada para implementar:
- Tema (claro/escuro)
- Idioma
- Configurações de layout
- Filtros salvos por página
- Colunas visíveis em tabelas

### Melhorias de Performance

- Virtualização em listas grandes
- Otimização de re-renders
- Code splitting mais granular
- Lazy loading de imagens

### Testes

- Testes unitários para helpers
- Testes de integração para fluxos principais
- Testes E2E para autenticação e navegação

## 📄 Licença

Este projeto é privado.

## 👥 Contribuição

Este é um projeto interno. Para contribuir, entre em contato com a equipe de desenvolvimento.

---

**Desenvolvido com ❤️ usando React e Ant Design**
